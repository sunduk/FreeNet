using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Core class of FreeNet. Servers call <see cref="Listen"/> to accept clients; clients use <see cref="Connector"/>
/// which calls <see cref="OnConnectCompleted"/> on success. Each connection is represented by a <see cref="UserToken"/>
/// whose async I/O is driven by <c>System.IO.Pipelines</c> started via <see cref="UserToken.StartPipelinesAsync"/>.
/// </summary>
public class NetworkService
{
    /// <summary>
    /// Server-wide cancellation source; cancel this to stop all connections.
    /// </summary>
    private readonly CancellationTokenSource _serverCancellation = new();

    /// <summary>
    /// Set <paramref name="useLogicThread"/> to <c>true</c> to process incoming packets on a single dedicated logic
    /// thread. Set it to <c>false</c> to process packets directly on the async I/O tasks.
    /// </summary>
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
    /// Raised when a new session is fully initialised and ready for application use.
    /// </summary>
    public event EventHandler<SessionEventArgs>? SessionCreated;

    /// <summary>
    /// Gets the logic-thread dispatcher, or <c>null</c> when not used.
    /// </summary>
    public LogicMessageEntry? LogicEntry { get; private set; }

    /// <summary>
    /// Gets the connected-user registry.
    /// </summary>
    public ServerUserManager Usermanager { get; private set; }

    /// <summary>
    /// Stops the periodic heartbeat checker.
    /// </summary>
    public void DisableHeartbeat() => Usermanager.StopHeartbeatChecking();

    /// <summary>
    /// Starts accepting clients and the heartbeat monitor.
    /// </summary>
    public void Listen(string host, int port, int backlog)
    {
        var clientListener = new Listener();
        clientListener.NewClientConnected += OnNewClientConnected;
        clientListener.Start(host, port, backlog);

        const byte checkInterval = 10;
        Usermanager.StartHeartbeatChecking(checkInterval, checkInterval);
    }

    /// <summary>
    /// Called after a client-side connect succeeds (from <see cref="Connector"/> ). Registers the token, wires the
    /// session-closed event, and starts async I/O.
    /// </summary>
    public void OnConnectCompleted(Socket socket, UserToken token)
    {
        token.SessionClosed += OnSessionClosed;
        token.Socket = socket;
        token.OnConnected();

        Usermanager.Add(token);
        token.StartPipelinesAsync(_serverCancellation.Token);
    }

    /// <summary>
    /// Cancels all active connections by signalling the server cancellation token.
    /// </summary>
    public void StopServer() => _serverCancellation.Cancel();

    /// <summary>
    /// Invoked for each accepted client socket. Creates a <see cref="UserToken"/>, starts async I/O, raises
    /// <see cref="SessionCreated"/>, and sends the heartbeat start packet.
    /// </summary>
    private void OnNewClientConnected(object? sender, NewClientConnectedEventArgs? e)
    {
        if (e?.ClientSocket is null)
        {
            return;
        }

        UserToken userToken = new(LogicEntry!);
        userToken.SessionClosed += OnSessionClosed;
        userToken.Socket = e.ClientSocket;
        userToken.OnConnected();

        Usermanager.Add(userToken);
        userToken.StartPipelinesAsync(_serverCancellation.Token);

        SessionCreated?.Invoke(this, new SessionEventArgs(userToken));

        var msg = Packet.Create(UserToken.SYS_START_HEARTBEAT);
        const byte sendInterval = 5;
        msg.Push(sendInterval);
        userToken.Send(msg);
    }

    private void OnSessionClosed(object? sender, SessionEventArgs e)
    {
        Usermanager.Remove(e.Token);
    }
}
