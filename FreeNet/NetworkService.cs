using System;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// FreeNet의 핵심 클래스이다. 서버는 이 클래스를 상속받아 구현하고, 클라이언트는 이 클래스를 직접 사용한다. 서버는 listen()을 호출하여 클라이언트 접속을 기다리고, 클라이언트는 connect()를
/// 호출하여 서버에 접속한다. 접속이 성공하면 on_connect_completed()가 호출된다. 서버에서 새로운 클라이언트가 접속하면 on_new_client()가 호출된다. 이때 CUserToken이
/// 생성되어 세션이 시작된다. CUserToken은 소켓과 관련된 정보를 가지고 있으며, 메시지 송수신을 담당한다.
/// </summary>
public class NetworkService
{
    private SocketAsyncEventArgsPool _receiveEventArgsPool;
    private SocketAsyncEventArgsPool _sendEventArgsPool;

    /// <summary>
    /// 로직 스레드를 사용하려면 useLogicThread를 true로 설정한다. -&gt; 하나의 로직 스레드를 생성한다. -&gt; 메시지는 큐잉되어 싱글 스레드에서 처리된다. 로직 스레드를 사용하지
    /// 않으려면 useLogicThread를 false로 설정한다. -&gt; 별도의 로직 스레드는 생성하지 않는다. -&gt; IO스레드에서 직접 메시지 처리를 담당하게 된다.
    /// </summary>
    /// <param name="useLogicThread">true=Create single logic thread. false=Not use any logic thread.</param>
    public NetworkService(bool useLogicThread = false)
    {
        SessionCreatedCallback = null;
        Usermanager = new ServerUserManager();

        if (useLogicThread)
        {
            LogicEntry = new LogicMessageEntry(this);
            LogicEntry.Start();
        }
    }

    public delegate void SessionHandler(UserToken token);

    public LogicMessageEntry LogicEntry { get; private set; }
    public SessionHandler SessionCreatedCallback { get; set; }
    public ServerUserManager Usermanager { get; private set; }

    public void DisableHeartbeat() => Usermanager.StopHeartbeatChecking();

    public void Initialize()
    {
        // configs.
        var maxConnections = 10000;
        var bufferSize = 1024;
        Initialize(maxConnections, bufferSize);
    }

    /// <summary>
    /// Initializes the server by preallocating reusable buffers and context objects. These objects do not need to be
    /// preallocated or reused, but it is done this way to illustrate how the API can easily be used to create reusable
    /// objects to increase server performance.
    /// </summary>
    /// <param name="maxConnections">The maximum connections.</param>
    /// <param name="bufferSize">Size of the buffer.</param>
    public void Initialize(int maxConnections, int bufferSize)
    {
        // receive버퍼만 할당해 놓는다. send버퍼는 보낼때마다 할당하든 풀에서 얻어오든 하기 때문에.
        var preAllocCount = 1;

        BufferManager bufferManager = new(maxConnections * bufferSize * preAllocCount, bufferSize);
        _receiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnections);
        _sendEventArgsPool = new SocketAsyncEventArgsPool(maxConnections);

        // Allocates one large byte buffer which all I/O operations use a piece of. This gaurds
        // against memory fragmentation
        bufferManager.InitBuffer();

        // preallocate pool of SocketAsyncEventArgs objects
        SocketAsyncEventArgs arg;

        for (var i = 0; i < maxConnections; i++)
        {
            // 더이상 UserToken을 미리 생성해 놓지 않는다. 다수의 클라이언트에서 접속 -> 메시지 송수신 -> 접속 해제를 반복할 경우 문제가 생김. 일단
            // on_new_client에서 그때 그때 생성하도록 하고, 소켓이 종료되면 null로 세팅하여 오류 발생시 확실히 드러날 수 있도록 코드를 변경한다.

            // receive pool
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
                arg.UserToken = null;

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                _ = bufferManager.SetBuffer(arg);

                // add SocketAsyncEventArg to the pool
                _receiveEventArgsPool.Push(arg);
            }

            // send pool
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                arg.UserToken = null;

                // send버퍼는 보낼때 설정한다. SetBuffer가 아닌 BufferList를 사용.
                arg.SetBuffer(null, 0, 0);

                // add SocketAsyncEventArg to the pool
                _sendEventArgsPool.Push(arg);
            }
        }
    }

    /// <summary>
    /// Listens for incoming client connections.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="port">The port.</param>
    /// <param name="backlog">The backlog.</param>
    public void Listen(string host, int port, int backlog)
    {
        Listener client_listener = new();
        client_listener.CallbackOnNewClient += OnNewClient;
        client_listener.Start(host, port, backlog);

        // heartbeat.
        byte check_interval = 10;
        Usermanager.StartHeartbeatChecking(check_interval, check_interval);
    }

    /// <summary>
    /// 원격 서버에 접속 성공 했을 때 호출됩니다.
    /// </summary>
    /// <param name="socket">The socket.</param>
    /// <param name="token">The user token.</param>
    public void OnConnectCompleted(Socket socket, UserToken token)
    {
        token.OnSessionClosed += OnSessionClosed;
        Usermanager.Add(token);

        // SocketAsyncEventArgsPool에서 빼오지 않고 그때 그때 할당해서 사용한다. 풀은 서버에서 클라이언트와의 통신용으로만 쓰려고 만든것이기 때문이다.
        // 클라이언트 입장에서 서버와 통신을 할 때는 접속한 서버당 두개의 EventArgs만 있으면 되기 때문에 그냥 new해서 쓴다. 서버간 연결에서도 마찬가지이다.
        // 풀링처리를 하려면 c->s로 가는 별도의 풀을 만들어서 써야 한다.
        SocketAsyncEventArgs receiveEventArg = new();
        receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
        receiveEventArg.UserToken = token;
        receiveEventArg.SetBuffer(new byte[1024], 0, 1024);

        SocketAsyncEventArgs sendEventArg = new();
        sendEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
        sendEventArg.UserToken = token;
        sendEventArg.SetBuffer(null, 0, 0);

        BeginReceive(socket, receiveEventArg, sendEventArg);
    }

    private static void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
    {
        // receiveArgs, sendArgs 아무곳에서나 꺼내와도 된다. 둘다 동일한 CUserToken을 물고 있다.
        var token = receiveArgs.UserToken as UserToken;
        token.SetEventArgs(receiveArgs, sendArgs);
        // 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
        token.Socket = socket;

        var pending = socket.ReceiveAsync(receiveArgs);
        if (!pending)
        {
            ProcessReceive(receiveArgs);
        }
    }

    /// <summary>
    /// This method is invoked when an asynchronous receive operation completes. If the remote host closed the
    /// connection, then the socket is closed.
    /// </summary>
    /// <param name="e">The <see cref="SocketAsyncEventArgs"/> instance containing the event data.</param>
    private static void ProcessReceive(SocketAsyncEventArgs e)
    {
        if (e.UserToken is not UserToken token)
        {
            return;
        }

        while (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            token.OnReceive(e.Buffer, e.Offset, e.BytesTransferred);

            // Keep receive.
            var pending = token.Socket.ReceiveAsync(e);
            if (pending)
            {
                return;
            }
        }

        try
        {
            token.Close();
        }
        catch (Exception)
        {
            Console.WriteLine("Already closed this socket.");
        }
    }

    /// <summary>
    /// 새로운 클라이언트가 접속 성공 했을 때 호출됩니다. AcceptAsync의 콜백 매소드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에 공유자원에 접근할 때는 주의해야 합니다.
    /// </summary>
    /// <param name="clientSocket"></param>
    private void OnNewClient(Socket clientSocket, object token)
    {
        // 플에서 하나 꺼내와 사용한다.
        var receiveArgs = _receiveEventArgsPool.Pop();
        var sendArgs = _sendEventArgsPool.Pop();

        // UserToken은 매번 새로 생성하여 깨끗한 인스턴스로 넣어준다.
        UserToken userToken = new(LogicEntry);
        userToken.OnSessionClosed += OnSessionClosed;
        receiveArgs.UserToken = userToken;
        sendArgs.UserToken = userToken;

        Usermanager.Add(userToken);

        userToken.OnConnected();
        SessionCreatedCallback?.Invoke(userToken);

        BeginReceive(clientSocket, receiveArgs, sendArgs);

        var msg = Packet.Create(UserToken.SYS_START_HEARTBEAT);
        byte send_interval = 5;
        msg.Push(send_interval);
        userToken.Send(msg);
    }

    /// <summary>
    /// This method is called whenever a receive or send operation is completed on a socket
    /// </summary>
    /// <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            ProcessReceive(e);
            return;
        }

        throw new ArgumentException("The last operation completed on the socket was not a receive.");
    }

    /// <summary>
    /// This method is called whenever a receive or send operation is completed on a socket
    /// </summary>
    /// <param name="e">SocketAsyncEventArg associated with the completed send operation</param>
    private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
    {
        try
        {
            var token = e.UserToken as UserToken;
            token.ProcessSend(e);
        }
        catch (Exception)
        {
        }
    }

    private void OnSessionClosed(UserToken token)
    {
        Usermanager.Remove(token);

        // Free the SocketAsyncEventArg so they can be reused by another client 버퍼는 반환할 필요가 없다.
        // SocketAsyncEventArg가 버퍼를 물고 있기 때문에 이것을 재사용 할 때 물고 있는 버퍼를 그대로 사용하면 되기 때문이다.
        _receiveEventArgsPool?.Push(token.ReceiveEventArgs);

        _sendEventArgsPool?.Push(token.SendEventArgs);

        token.SetEventArgs(null, null);
    }
}