using System.IO.Pipelines;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Represents a user connection. I/O is handled by the async Pipelines loops.
/// </summary>
public partial class UserToken(IMessageDispatcher? dispatcher = null)
{
    /// <summary>
    /// Starts heartbeat. S → C
    /// </summary>
    public const short SYS_START_HEARTBEAT = -2;

    /// <summary>
    /// Updates heartbeat. C → S
    /// </summary>
    public const short SYS_UPDATE_HEARTBEAT = -3;

    private const short SYS_CLOSE_ACK = -1;

    private const short SYS_CLOSE_REQ = 0;

    private readonly MessageResolver _messageResolver = new();

    private readonly Pipe _sendPipe = new();

    private bool _autoHeartbeat;

    private State _currentState = State.Idle;

    private HeartbeatSender? _heartbeatSender;

    private CancellationTokenSource? _ioCancellation;

    /// <summary>
    /// Flag to prevent duplicate close handling. 0 = connected. 1 = closed.
    /// </summary>
    private int _isClosed;

    private Task? _receiveLoopTask;
    private Task? _sendLoopTask;

    /// <summary>
    /// Session closed event. Callback method invoked when the session ends.
    /// </summary>
    public event EventHandler<SessionEventArgs>? SessionClosed;

    /// <summary>
    /// Gets the latest heartbeat time.
    /// </summary>
    public long LatestHeartbeatTime { get; private set; } = DateTime.Now.Ticks;

    /// <summary>
    /// Gets or sets the peer.
    /// </summary>
    public IPeer? Peer { private get; set; }

    /// <summary>
    /// Gets or sets the socket.
    /// </summary>
    public Socket? Socket { get; set; }

    /// <summary>
    /// Ends the connection by sending a close code and letting the remote side disconnect first. Prefer this over
    /// <see cref="Close"/> on the server side to avoid leaving TIME_WAIT.
    /// </summary>
    public void Ban()
    {
        try
        {
            ByeBye();
        }
        catch (Exception)
        {
            Close();
        }
    }

    /// <summary>
    /// Immediately closes the connection and notifies the peer.
    /// </summary>
    public void Close()
    {
        // Prevent duplicate execution.
        if (Interlocked.CompareExchange(ref _isClosed, 1, 0) == 1)
        {
            return;
        }

        if (_currentState == State.Closed)
        {
            return;
        }

        _currentState = State.Closed;

        // Cancel async I/O loops.
        try
        {
            _ioCancellation?.Cancel();
        }
        catch (Exception)
        {
            // ignored
        }

        Socket?.Close();
        Socket = null;

        _messageResolver.ClearBuffer();

        if (Peer is not null)
        {
            var msg = Packet.Create(-1);
            if (dispatcher is not null)
            {
                dispatcher.OnMessage(this, new ArraySegment<byte>(msg.Buffer ?? [], 0, msg.Position));
            }
            else
            {
                OnMessage(msg); // fires SessionClosed internally via SYS_CLOSE_ACK path
            }
        }
        else
        {
            // No peer registered, but still notify session-level listeners (e.g. NetworkService).
            SessionClosed?.Invoke(this, new SessionEventArgs(this));
        }
    }

    public void DisableAutoHeartbeat()
    {
        StopHeartbeat();
        _autoHeartbeat = false;
    }

    /// <summary>
    /// Initiates a graceful disconnect by completing the send pipe so the send loop drains remaining data before
    /// issuing the TCP half-close.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            if (_ioCancellation is null)
            {
                // Pipelines not started; fall back to immediate half-close.
                Socket?.Shutdown(SocketShutdown.Send);
                return;
            }

            // Completing the writer signals the send loop to drain then shut down.
            _sendPipe.Writer.Complete();
        }
        catch (Exception)
        {
            Close();
        }
    }

    /// <returns><c>true</c> if the connection is active.</returns>
    public bool IsConnected() => _currentState == State.Connected;

    /// <summary>
    /// Called when the connection is established.
    /// </summary>
    public void OnConnected()
    {
        _currentState = State.Connected;
        _isClosed = 0;
        _autoHeartbeat = true;
    }

    /// <summary>
    /// Dispatches a fully assembled packet. Handles system protocol IDs and forwards application packets to the
    /// registered <see cref="IPeer"/>.
    /// </summary>
    public void OnMessage(Packet message)
    {
        switch (message.ProtocolId)
        {
            case SYS_CLOSE_REQ:
                Disconnect();
                return;

            case SYS_START_HEARTBEAT:
                _ = message.PopProtocolId();
                var interval = message.PopByte();
                _heartbeatSender = new HeartbeatSender(this, interval);
                if (_autoHeartbeat)
                {
                    StartHeartbeat();
                }

                return;

            case SYS_UPDATE_HEARTBEAT:
                LatestHeartbeatTime = DateTime.Now.Ticks;
                return;
        }

        if (Peer is not null)
        {
            try
            {
                switch (message.ProtocolId)
                {
                    case SYS_CLOSE_ACK:
                        Peer.OnRemoved();
                        break;

                    default:
                        Peer.OnMessage(message);
                        break;
                }
            }
            catch (Exception)
            {
                Close();
            }
        }

        if (message.ProtocolId == SYS_CLOSE_ACK)
        {
            SessionClosed?.Invoke(this, new SessionEventArgs(this));
        }
    }

    /// <summary>
    /// Sends a packet.
    /// </summary>
    public void Send(Packet message)
    {
        message.RecordSize();
        Send(new ArraySegment<byte>(message.Buffer ?? [], 0, message.Position));
    }

    /// <summary>
    /// Sends a segment of bytes.
    /// </summary>
    public void Send(ArraySegment<byte> data)
    {
        try
        {
            var flushTask = _sendPipe.Writer.WriteAsync(
                new ReadOnlyMemory<byte>(data.Array, data.Offset, data.Count));

            if (!flushTask.IsCompletedSuccessfully)
            {
                _ = flushTask.AsTask();
            }
        }
        catch (Exception)
        {
            Close();
        }
    }

    /// <summary>
    /// Starts the heartbeat sender.
    /// </summary>
    public void StartHeartbeat() => _heartbeatSender?.Play();

    /// <summary>
    /// Starts the pipelines asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public void StartPipelinesAsync(CancellationToken cancellationToken = default)
    {
        if (_ioCancellation is not null)
        {
            return;
        }

        _ioCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ioToken = _ioCancellation.Token;

        _receiveLoopTask = ReceiveLoopAsync(ioToken);
        _sendLoopTask = SendLoopAsync(ioToken);
    }

    /// <summary>
    /// Stops the heartbeat.
    /// </summary>
    public void StopHeartbeat() => _heartbeatSender?.Stop();

    /// <summary>
    /// Updates the heartbeat manually.
    /// </summary>
    /// <param name="time">The time.</param>
    public void UpdateHeartbeatManually(float time) => _heartbeatSender?.Update(time);

    /// <summary>
    /// Feeds raw bytes into the message resolver. Used by the receive loop and by unit tests to simulate incoming data
    /// without a live socket.
    /// </summary>
    internal void OnReceive(byte[] buffer, int offset, int transferred) =>
        _messageResolver.OnReceive(buffer, offset, transferred, OnMessageCompleted);

    /// <summary>
    /// Sends a SYS_CLOSE_REQ so the remote side closes first.
    /// </summary>
    private void ByeBye()
    {
        var bye = Packet.Create(SYS_CLOSE_REQ);
        Send(bye);
    }

    private void OnMessageCompleted(ArraySegment<byte> buffer)
    {
        if (Peer is null)
        {
            return;
        }

        if (dispatcher is not null)
        {
            dispatcher.OnMessage(this, buffer);
        }
        else
        {
            Packet msg = new(buffer, this);
            OnMessage(msg);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (Socket is null)
        {
            return;
        }

        var receiveBuffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && Socket is not null)
            {
                int bytesReceived;
                try
                {
                    bytesReceived = await Socket
                        .ReceiveAsync(new Memory<byte>(receiveBuffer), SocketFlags.None, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (SocketException)
                {
                    Close();
                    return;
                }

                if (bytesReceived == 0)
                {
                    Close();
                    return;
                }

                OnReceive(receiveBuffer, 0, bytesReceived);
            }
        }
        finally
        {
            Close();
        }
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        if (Socket is null)
        {
            return;
        }

        var reader = _sendPipe.Reader;

        try
        {
            while (!cancellationToken.IsCancellationRequested && Socket is not null)
            {
                ReadResult result;
                try
                {
                    result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (result.Buffer.Length > 0 && Socket is not null)
                {
                    foreach (var segment in result.Buffer)
                    {
                        if (Socket is null)
                        {
                            break;
                        }

                        try
                        {
                            _ = await Socket
                                .SendAsync(segment, SocketFlags.None, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (SocketException)
                        {
                            Close();
                            return;
                        }
                    }
                }

                reader.AdvanceTo(result.Buffer.End);

                if (result.IsCompleted)
                {
                    try { Socket?.Shutdown(SocketShutdown.Send); }
                    catch (Exception) { }

                    break;
                }
            }
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
            Close();
        }
    }
}
