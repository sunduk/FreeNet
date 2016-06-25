using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
    public class CNetworkService
    {
		int connected_count;
		CListener client_listener;
		SocketAsyncEventArgsPool receive_event_args_pool;
		SocketAsyncEventArgsPool send_event_args_pool;
		BufferManager buffer_manager;

		public delegate void SessionHandler(CUserToken token);
		public SessionHandler session_created_callback { get; set; }

		// configs.
		int max_connections;
		int buffer_size;
		readonly int pre_alloc_count = 2;		// read, write

		public CNetworkService()
		{
			this.connected_count = 0;
			this.session_created_callback = null;
		}

		// Initializes the server by preallocating reusable buffers and 
		// context objects.  These objects do not need to be preallocated 
		// or reused, but it is done this way to illustrate how the API can 
		// easily be used to create reusable objects to increase server performance.
		//
		public void initialize()
		{
			this.max_connections = 10000;
			this.buffer_size = 1024;

			this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
			this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
			this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);

			// Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
			// against memory fragmentation
			this.buffer_manager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			SocketAsyncEventArgs arg;

			for (int i = 0; i < this.max_connections; i++)
			{
				// 동일한 소켓에 대고 send, receive를 하므로
				// user token은 세션별로 하나씩만 만들어 놓고 
				// receive, send EventArgs에서 동일한 token을 참조하도록 구성한다.
				CUserToken token = new CUserToken();

				// receive pool
				{
					//Pre-allocate a set of reusable SocketAsyncEventArgs
					arg = new SocketAsyncEventArgs();
					arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
					arg.UserToken = token;

					// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
					this.buffer_manager.SetBuffer(arg);

					// add SocketAsyncEventArg to the pool
					this.receive_event_args_pool.Push(arg);
				}


				// send pool
				{
					//Pre-allocate a set of reusable SocketAsyncEventArgs
					arg = new SocketAsyncEventArgs();
					arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
					arg.UserToken = token;

					// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
					this.buffer_manager.SetBuffer(arg);

					// add SocketAsyncEventArg to the pool
					this.send_event_args_pool.Push(arg);
				}
			}
		}

		public void listen(string host, int port, int backlog)
		{
			this.client_listener = new CListener();
			this.client_listener.callback_on_newclient += on_new_client;
			this.client_listener.start(host, port, backlog);
		}

		/// <summary>
		/// todo:검토중...
		/// 원격 서버에 접속 성공 했을 때 호출됩니다.
		/// </summary>
		/// <param name="socket"></param>
		public void on_connect_completed(Socket socket, CUserToken token)
		{
			// SocketAsyncEventArgsPool에서 빼오지 않고 그때 그때 할당해서 사용한다.
			// 풀은 서버에서 클라이언트와의 통신용으로만 쓰려고 만든것이기 때문이다.
			// 클라이언트 입장에서 서버와 통신을 할 때는 접속한 서버당 두개의 EventArgs만 있으면 되기 때문에 그냥 new해서 쓴다.
			// 서버간 연결에서도 마찬가지이다.
			// 풀링처리를 하려면 c->s로 가는 별도의 풀을 만들어서 써야 한다.
			SocketAsyncEventArgs receive_event_arg = new SocketAsyncEventArgs();
			receive_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
			receive_event_arg.UserToken = token;
			receive_event_arg.SetBuffer(new byte[1024], 0, 1024);

			SocketAsyncEventArgs send_event_arg = new SocketAsyncEventArgs();
			send_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
			send_event_arg.UserToken = token;
			send_event_arg.SetBuffer(new byte[1024], 0, 1024);

			begin_receive(socket, receive_event_arg, send_event_arg);
		}

        /// <summary>
        /// 새로운 클라이언트가 접속 성공 했을 때 호출됩니다.
        /// AcceptAsync의 콜백 매소드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에 공유자원에 접근할 때는 주의해야 합니다.
        /// </summary>
        /// <param name="client_socket"></param>
		void on_new_client(Socket client_socket, object token)
		{
            //todo:
            // peer list처리.

			Interlocked.Increment(ref this.connected_count);

			Console.WriteLine(string.Format("[{0}] A client connected. handle {1},  count {2}",
				Thread.CurrentThread.ManagedThreadId, client_socket.Handle,
				this.connected_count));

			// 플에서 하나 꺼내와 사용한다.
			SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
			SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

			CUserToken user_token = null;
			if (this.session_created_callback != null)
			{
				user_token = receive_args.UserToken as CUserToken;
				this.session_created_callback(user_token);
			}

			begin_receive(client_socket, receive_args, send_args);
			//user_token.start_keepalive();
		}

		void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
		{
			// receive_args, send_args 아무곳에서나 꺼내와도 된다. 둘다 동일한 CUserToken을 물고 있다.
			CUserToken token = receive_args.UserToken as CUserToken;
			token.set_event_args(receive_args, send_args);
			// 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
			token.socket = socket;

			bool pending = socket.ReceiveAsync(receive_args);
			if (!pending)
			{
				process_receive(receive_args);
			}
		}

		// This method is called whenever a receive or send operation is completed on a socket 
		//
		// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
		void receive_completed(object sender, SocketAsyncEventArgs e)
		{
			if (e.LastOperation == SocketAsyncOperation.Receive)
			{
				process_receive(e);
				return;
			}

			throw new ArgumentException("The last operation completed on the socket was not a receive.");
		}

		// This method is called whenever a receive or send operation is completed on a socket 
		//
		// <param name="e">SocketAsyncEventArg associated with the completed send operation</param>
		void send_completed(object sender, SocketAsyncEventArgs e)
		{
			CUserToken token = e.UserToken as CUserToken;
			token.process_send(e);
		}

		// This method is invoked when an asynchronous receive operation completes. 
		// If the remote host closed the connection, then the socket is closed.  
		//
		private void process_receive(SocketAsyncEventArgs e)
		{
			// check if the remote host closed the connection
			CUserToken token = e.UserToken as CUserToken;
			if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
			{
				token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

				bool pending = token.socket.ReceiveAsync(e);
				if (!pending)
				{
					// Oh! stack overflow??
					process_receive(e);
				}
			}
			else
			{
				Console.WriteLine(string.Format("error {0},  transferred {1}", e.SocketError, e.BytesTransferred));
				close_clientsocket(token);
			}
		}

		public void close_clientsocket(CUserToken token)
		{
			token.on_removed();

			// Free the SocketAsyncEventArg so they can be reused by another client
			// 버퍼는 반환할 필요가 없다. SocketAsyncEventArg가 버퍼를 물고 있기 때문에
			// 이것을 재사용 할 때 물고 있는 버퍼를 그대로 사용하면 되기 때문이다.
			if (this.receive_event_args_pool != null)
			{
				this.receive_event_args_pool.Push(token.receive_event_args);
			}

			if (this.send_event_args_pool != null)
			{
				this.send_event_args_pool.Push(token.send_event_args);
			}
		}
    }
}
