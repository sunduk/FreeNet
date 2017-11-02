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
		SocketAsyncEventArgsPool receive_event_args_pool;
		SocketAsyncEventArgsPool send_event_args_pool;

		public delegate void SessionHandler(CUserToken token);
		public SessionHandler session_created_callback { get; set; }

        public CLogicMessageEntry logic_entry { get; private set; }
        public CServerUserManager usermanager { get; private set; }


        /// <summary>
        /// 로직 스레드를 사용하려면 use_logicthread를 true로 설정한다.
        ///  -> 하나의 로직 스레드를 생성한다.
        ///  -> 메시지는 큐잉되어 싱글 스레드에서 처리된다.
        /// 
        /// 로직 스레드를 사용하지 않으려면 use_logicthread를 false로 설정한다.
        ///  -> 별도의 로직 스레드는 생성하지 않는다.
        ///  -> IO스레드에서 직접 메시지 처리를 담당하게 된다.
        /// </summary>
        /// <param name="use_logicthread">true=Create single logic thread. false=Not use any logic thread.</param>
		public CNetworkService(bool use_logicthread = false)
		{
			this.session_created_callback = null;
            this.usermanager = new CServerUserManager();

            if (use_logicthread)
            {
                this.logic_entry = new CLogicMessageEntry(this);
                this.logic_entry.start();
            }
        }


        public void initialize()
        {
            // configs.
            int max_connections = 10000;
            int buffer_size = 1024;
            initialize(max_connections, buffer_size);
        }

		// Initializes the server by preallocating reusable buffers and 
		// context objects.  These objects do not need to be preallocated 
		// or reused, but it is done this way to illustrate how the API can 
		// easily be used to create reusable objects to increase server performance.
		//
		public void initialize(int max_connections, int buffer_size)
		{
            // receive버퍼만 할당해 놓는다.
            // send버퍼는 보낼때마다 할당하든 풀에서 얻어오든 하기 때문에.
            int pre_alloc_count = 1;

			BufferManager buffer_manager = new BufferManager(max_connections * buffer_size * pre_alloc_count, buffer_size);
			this.receive_event_args_pool = new SocketAsyncEventArgsPool(max_connections);
			this.send_event_args_pool = new SocketAsyncEventArgsPool(max_connections);

			// Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
			// against memory fragmentation
			buffer_manager.InitBuffer();

			// preallocate pool of SocketAsyncEventArgs objects
			SocketAsyncEventArgs arg;

            for (int i = 0; i < max_connections; i++)
			{
                // 더이상 UserToken을 미리 생성해 놓지 않는다.
                // 다수의 클라이언트에서 접속 -> 메시지 송수신 -> 접속 해제를 반복할 경우 문제가 생김.
                // 일단 on_new_client에서 그때 그때 생성하도록 하고,
                // 소켓이 종료되면 null로 세팅하여 오류 발생시 확실히 드러날 수 있도록 코드를 변경한다.

                // receive pool
                {
                    //Pre-allocate a set of reusable SocketAsyncEventArgs
                    arg = new SocketAsyncEventArgs();
					arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
					arg.UserToken = null;

					// assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
					buffer_manager.SetBuffer(arg);

					// add SocketAsyncEventArg to the pool
					this.receive_event_args_pool.Push(arg);
				}


				// send pool
				{
					//Pre-allocate a set of reusable SocketAsyncEventArgs
					arg = new SocketAsyncEventArgs();
					arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
					arg.UserToken = null;

                    // send버퍼는 보낼때 설정한다. SetBuffer가 아닌 BufferList를 사용.
                    arg.SetBuffer(null, 0, 0);

					// add SocketAsyncEventArg to the pool
					this.send_event_args_pool.Push(arg);
				}
			}
        }

		public void listen(string host, int port, int backlog)
		{
			CListener client_listener = new CListener();
			client_listener.callback_on_newclient += on_new_client;
			client_listener.start(host, port, backlog);

            // heartbeat.
            byte check_interval = 10;
            this.usermanager.start_heartbeat_checking(check_interval, check_interval);
        }

        public void disable_heartbeat()
        {
            this.usermanager.stop_heartbeat_checking();
        }

		/// <summary>
		/// 원격 서버에 접속 성공 했을 때 호출됩니다.
		/// </summary>
		/// <param name="socket"></param>
		public void on_connect_completed(Socket socket, CUserToken token)
		{
            token.on_session_closed += this.on_session_closed;
            this.usermanager.add(token);

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
			send_event_arg.SetBuffer(null, 0, 0);

			begin_receive(socket, receive_event_arg, send_event_arg);
		}

        /// <summary>
        /// 새로운 클라이언트가 접속 성공 했을 때 호출됩니다.
        /// AcceptAsync의 콜백 매소드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에 공유자원에 접근할 때는 주의해야 합니다.
        /// </summary>
        /// <param name="client_socket"></param>
		void on_new_client(Socket client_socket, object token)
		{
			// 플에서 하나 꺼내와 사용한다.
			SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
			SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            // UserToken은 매번 새로 생성하여 깨끗한 인스턴스로 넣어준다.
            CUserToken user_token = new CUserToken(this.logic_entry);
            user_token.on_session_closed += this.on_session_closed;
            receive_args.UserToken = user_token;
            send_args.UserToken = user_token;

            this.usermanager.add(user_token);

            user_token.on_connected();
            if (this.session_created_callback != null)
			{
				this.session_created_callback(user_token);
			}

			begin_receive(client_socket, receive_args, send_args);

            CPacket msg = CPacket.create((short)CUserToken.SYS_START_HEARTBEAT);
            byte send_interval = 5;
            msg.push(send_interval);
            user_token.send(msg);
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
            try
            {
                CUserToken token = e.UserToken as CUserToken;
                token.process_send(e);
            }
            catch (Exception)
            {
            }
		}

		// This method is invoked when an asynchronous receive operation completes. 
		// If the remote host closed the connection, then the socket is closed.  
		//
		private void process_receive(SocketAsyncEventArgs e)
		{
            CUserToken token = e.UserToken as CUserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

                // Keep receive.
                bool pending = token.socket.ReceiveAsync(e);
                if (!pending)
                {
                    // Oh! stack overflow??
                    process_receive(e);
                }
            }
            else
            {
                try
                {
                    token.close();
                }
                catch (Exception)
                {
                    Console.WriteLine("Already closed this socket.");
                }
            }
		}

        void on_session_closed(CUserToken token)
        {
            this.usermanager.remove(token);

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

            token.set_event_args(null, null);
        }
    }
}
