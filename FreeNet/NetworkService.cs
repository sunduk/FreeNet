using System;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Core class of FreeNet.
/// Servers typically build on this class, while clients use it directly.
/// The server calls listen() to accept clients, and the client calls connect() to connect to a server.
/// On successful connect, on_connect_completed() is called.
/// When a new client connects to the server, on_new_client() is called and a CUserToken is created to start a session.
/// CUserToken holds socket-related state and handles message send/receive.
/// </summary>
public class NetworkService
{
    private SocketAsyncEventArgsPool _receiveEventArgsPool;
    private SocketAsyncEventArgsPool _sendEventArgsPool;

    /// <summary>
    /// Set useLogicThread=true to create one logic thread and process queued messages on that single thread.
    /// Set useLogicThread=false to avoid creating a separate logic thread and process messages directly on I/O threads.
    /// </summary>
    /// <param name="useLogicThread">true=Create single logic thread. false=Not use any logic thread.</param>
    public NetworkService(bool useLogicThread = false)
    {
        Usermanager = new ServerUserManager();

        if (useLogicThread)
        {
            LogicEntry = new LogicMessageEntry(this);
            LogicEntry.Start();
        }
    }

    /// <summary>
    /// Delegate SessionHandler
    /// </summary>
    /// <param name="token">The user token.</param>
    public delegate void SessionHandler(UserToken token);

    /// <summary>
    /// Gets the logic entry.
    /// </summary>
    /// <value>The logic entry.</value>
    public LogicMessageEntry LogicEntry { get; private set; }

    /// <summary>
    /// Gets or sets the session created callback.
    /// </summary>
    /// <value>The session created callback.</value>
    public event SessionHandler? SessionCreatedCallback;

    /// <summary>
    /// Gets the usermanager.
    /// </summary>
    /// <value>The usermanager.</value>
    public ServerUserManager Usermanager { get; private set; }

    /// <summary>
    /// Disables the heartbeat checking.
    /// </summary>
    public void DisableHeartbeat() => Usermanager.StopHeartbeatChecking();

    /// <summary>
    /// Initializes this instance.
    /// </summary>
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
        // Only preallocate receive buffers. Send buffers are set per send or obtained from a pool.
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
            // Do not pre-create UserToken instances anymore.
            // Repeated connect/message/disconnect cycles across many clients caused issues.
            // Create tokens per client in on_new_client and set to null when sockets close so failures are explicit.

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

                // Set send buffers at send time. Use BufferList instead of SetBuffer.
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
    /// Called when connection to a remote server succeeds.
    /// </summary>
    /// <param name="socket">The socket.</param>
    /// <param name="token">The user token.</param>
    public void OnConnectCompleted(Socket socket, UserToken token)
    {
        token.OnSessionClosed += OnSessionClosed;
        Usermanager.Add(token);

        // Allocate event args on demand instead of taking from SocketAsyncEventArgsPool.
        // That pool is intended for server-to-client communication.
        // From a client perspective, two EventArgs per connected server are enough, so plain new is used.
        // For pooling client->server paths, create a separate pool.
        SocketAsyncEventArgs receiveEventArg = new();
        receiveEventArg.Completed += OnReceiveCompleted;
        receiveEventArg.UserToken = token;
        receiveEventArg.SetBuffer(new byte[1024], 0, 1024);

        SocketAsyncEventArgs sendEventArg = new();
        sendEventArg.Completed += OnSendCompleted;
        sendEventArg.UserToken = token;
        sendEventArg.SetBuffer(null, 0, 0);

        BeginReceive(socket, receiveEventArg, sendEventArg);
    }

    private static void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
    {
        // Either receiveArgs or sendArgs can be used here; both reference the same CUserToken.
        var token = receiveArgs.UserToken as UserToken;
        token.SetEventArgs(receiveArgs, sendArgs);
        // Store the created client socket for subsequent communication.
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
    /// Called when a new client connection succeeds.
    /// Invoked from the AcceptAsync callback and may run concurrently on multiple threads,
    /// so shared-resource access must be handled carefully.
    /// </summary>
    /// <param name="clientSocket"></param>
    private void OnNewClient(Socket clientSocket, object token)
    {
        // Pop one entry from each pool and use it.
        var receiveArgs = _receiveEventArgsPool.Pop();
        var sendArgs = _sendEventArgsPool.Pop();

        // Create a fresh UserToken instance for every new connection.
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

        // Return SocketAsyncEventArg objects to the pool for reuse by other clients.
        // No separate buffer return is needed because SocketAsyncEventArg already holds its buffer.
        _receiveEventArgsPool?.Push(token.ReceiveEventArgs);

        _sendEventArgsPool?.Push(token.SendEventArgs);

        token.SetEventArgs(null, null);
    }
}
