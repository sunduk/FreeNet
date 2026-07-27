using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Represents a user connection.
/// </summary>
public class UserToken(IMessageDispatcher dispatcher)
{
    /// <summary>
    /// Starts heartbeat. S -&gt; C
    /// </summary>
    public const short SYS_START_HEARTBEAT = -2;

    /// <summary>
    /// Updates heartbeat. C -&gt; S
    /// </summary>
    public const short SYS_UPDATE_HEARTBEAT = -3;

    /// <summary>
    /// Session closed event. Callback method invoked when the session ends.
    /// </summary>
    public ClosedDelegate OnSessionClosed;

    /// <summary>
    /// Close acknowledgment. C -&gt; S
    /// </summary>
    private const short SYS_CLOSE_ACK = -1;

    /// <summary>
    /// Close request. S -&gt; C
    /// </summary>
    private const short SYS_CLOSE_REQ = 0;

    /// <summary>
    /// Resolver that interprets byte data as packets.
    /// </summary>
    private readonly MessageResolver _messageResolver = new();

    /// <summary>
    /// Changed from queue to list to support BufferList.
    /// </summary>
    private readonly List<ArraySegment<byte>> _sendingList = [];

    /// <summary>
    /// Object used for locking the sending list.
    /// </summary>
    private readonly Lock _sendingQueueLock = new();

    private bool _autoHeartbeat;
    private State _currentState = State.Idle;
    private HeartbeatSender _heartbeatSender;

    /// <summary>
    /// Flag to prevent duplicate close handling. 0 = connected. 1 = closed.
    /// </summary>
    private int _isClosed;

    /// <summary>
    /// Session object implemented by the application.
    /// </summary>
    private IPeer _peer = null;

    public delegate void ClosedDelegate(UserToken token);

    /// <summary>
    /// Enum representing the current connection state.
    /// </summary>
    private enum State
    {
        /// <summary>
        /// Idle.
        /// </summary>
        Idle,

        /// <summary>
        /// Connected.
        /// </summary>
        Connected,

        /// <summary>
        /// Closing is reserved. If disconnect is called while items remain in the sending list,
        /// this state ensures the connection closes after all remaining packets are sent.
        /// </summary>
        ReserveClosing,

        /// <summary>
        /// Socket is fully closed.
        /// </summary>
        Closed,
    }

    /// <summary>
    /// Gets the latest heartbeat time.
    /// </summary>
    /// <value>The latest heartbeat time.</value>
    public long LatestHeartbeatTime { get; private set; } = DateTime.Now.Ticks;

    /// <summary>
    /// Gets the receive event arguments.
    /// </summary>
    /// <value>The receive event arguments.</value>
    public SocketAsyncEventArgs ReceiveEventArgs { get; private set; }

    /// <summary>
    /// Gets the send event arguments.
    /// </summary>
    /// <value>The send event arguments.</value>
    public SocketAsyncEventArgs SendEventArgs { get; private set; }

    /// <summary>
    /// Gets or sets the socket.
    /// </summary>
    /// <value>The socket.</value>
    public Socket Socket { get; set; }

    /// <summary>
    /// Ends the connection by sending a close code and letting the remote side disconnect first.
    /// This is mainly used when the server disconnects a client. To avoid leaving TIME_WAIT on the
    /// server, use this method instead of Disconnect.
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

    public void Close()
    {
        // Prevent duplicate execution.
        if (Interlocked.CompareExchange(ref _isClosed, 1, 0) == 1)
        {
            return;
        }

        if (_currentState == State.Closed)
        {
            // already closed.
            return;
        }

        _currentState = State.Closed;
        Socket?.Close();
        Socket = null;

        SendEventArgs?.UserToken = null;

        ReceiveEventArgs?.UserToken = null;

        _sendingList.Clear();
        _messageResolver.ClearBuffer();

        if (_peer is not null)
        {
            var msg = Packet.Create(-1);
            if (dispatcher is not null)
            {
                dispatcher.OnMessage(this, new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
            }
            else
            {
                OnMessage(msg);
            }
        }
    }

    public void DisableAutoHeartbeat()
    {
        StopHeartbeat();
        _autoHeartbeat = false;
    }

    /// <summary>
    /// Ends the connection. Mainly called when the client disconnects.
    /// </summary>
    public void Disconnect()
    {
        // close the socket associated with the client
        try
        {
            if (_sendingList.Count <= 0)
            {
                Socket.Shutdown(SocketShutdown.Send);
                return;
            }

            _currentState = State.ReserveClosing;
        }
        // throws if client process has already closed
        catch (Exception)
        {
            Close();
        }
    }

    public bool IsConnected() => _currentState == State.Connected;

    public void OnConnected()
    {
        _currentState = State.Connected;
        _isClosed = 0;
        _autoHeartbeat = true;
    }

    public void OnMessage(Packet msg)
    {
        // Logic for active close: check whether the server requested shutdown. If the shutdown
        // signal is received, call Disconnect so the receiving side initiates close first.
        switch (msg.ProtocolId)
        {
            case SYS_CLOSE_REQ:
                Disconnect();
                return;

            case SYS_START_HEARTBEAT:
                {
                    // Parsing must happen in order, so discard the protocol ID.
                    _ = msg.PopProtocolId();
                    // Send interval.
                    var interval = msg.PopByte();
                    _heartbeatSender = new HeartbeatSender(this, interval);

                    if (_autoHeartbeat)
                    {
                        StartHeartbeat();
                    }
                }

                return;

            case SYS_UPDATE_HEARTBEAT:
                //Console.WriteLine("heartbeat : " + DateTime.Now);
                LatestHeartbeatTime = DateTime.Now.Ticks;
                return;
        }

        if (_peer is not null)
        {
            try
            {
                switch (msg.ProtocolId)
                {
                    case SYS_CLOSE_ACK:
                        _peer.OnRemoved();
                        break;

                    default:
                        _peer.OnMessage(msg);
                        break;
                }
            }
            catch (Exception)
            {
                Close();
            }
        }

        if (msg.ProtocolId == SYS_CLOSE_ACK)
        {
            if (OnSessionClosed is not null)
            {
                OnSessionClosed(this);
            }
        }
    }

    /// <summary>
    /// Byte data could be interpreted directly in this method, but the MessageResolver class is
    /// separated for extensibility so that implementing other resolvers later minimizes changes to
    /// the UserToken class.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="transfered"></param>
    public void OnReceive(byte[] buffer, int offset, int transfered) => _messageResolver.OnReceive(buffer, offset, transfered, OnMessageCompleted);

    /// <summary>
    /// Callback method invoked when asynchronous send completes.
    /// </summary>
    /// <param name="e"></param>
    public void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
        {
            // Explicitly close the session on send failure to prevent a half-open state.
            Close();
            return;
        }

        lock (_sendingQueueLock)
        {
            // Total number of bytes in the list.
            var size = _sendingList.Sum(obj => obj.Count);

            // If another send was requested before completion, sending_list will contain more data.
            if (e.BytesTransferred != size)
            {
                // TODO: Handle cases where a segment is only partially sent. For now, close it.
                if (e.BytesTransferred < _sendingList[0].Count)
                {
                    var error = string.Format("Need to send more! transferred {0},  packet size {1}", e.BytesTransferred, size);
                    Console.WriteLine(error);

                    Close();
                    return;
                }

                // Remove what was sent and send all remaining queued data in one shot.
                var sent_index = 0;
                var sum = 0;
                for (var i = 0; i < _sendingList.Count; ++i)
                {
                    sum += _sendingList[i].Count;
                    if (sum <= e.BytesTransferred)
                    {
                        // Up to this point are indexes of data already sent.
                        sent_index = i;
                        continue;
                    }

                    break;
                }
                // Remove sent items from the list.
                _sendingList.RemoveRange(0, sent_index + 1);

                // Send the remaining data in one shot.
                StartSend();
                return;
            }

            // Everything has been sent, and there is nothing else to send.
            _sendingList.Clear();

            // If closing was reserved, all sends are complete, so proceed with actual shutdown.
            if (_currentState == State.ReserveClosing)
            {
                Socket.Shutdown(SocketShutdown.Send);
            }
        }
    }

    /// <summary>
    /// Sends a packet. If the queue is empty, the data is added and SendAsync is called immediately.
    /// If data already exists, only append the new data. Queued packets are sent when the current
    /// SendAsync completes and the queue is checked for remaining data.
    /// </summary>
    /// <param name="msg"></param>
    public void Send(ArraySegment<byte> data)
    {
        lock (_sendingQueueLock)
        {
            _sendingList.Add(data);

            if (_sendingList.Count > 1)
            {
                // If the queue already has data, the previous send has not completed yet, so just
                // enqueue and return. After the current SendAsync completes, the queue is checked,
                // and SendAsync is called again if data remains.
                return;
            }
        }

        StartSend();
    }

    public void Send(Packet msg)
    {
        msg.RecordSize();
        Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
    }

    public void SetEventArgs(SocketAsyncEventArgs receive_event_args, SocketAsyncEventArgs send_event_args)
    {
        ReceiveEventArgs = receive_event_args;
        SendEventArgs = send_event_args;
    }

    public void SetPeer(IPeer peer) => _peer = peer;

    public void StartHeartbeat() => _heartbeatSender?.Play();

    public void StopHeartbeat() => _heartbeatSender?.Stop();

    public void UpdateHeartbeatManually(float time) => _heartbeatSender?.Update(time);

    /// <summary>
    /// Sends a close code so the remote side disconnects first.
    /// </summary>
    private void ByeBye()
    {
        var bye = Packet.Create(SYS_CLOSE_REQ);
        Send(bye);
    }

    private void OnMessageCompleted(ArraySegment<byte> buffer)
    {
        if (_peer is null)
        {
            return;
        }

        if (dispatcher is not null)
        {
            // Ensure this is invoked through the logic thread queue.
            dispatcher.OnMessage(this, buffer);
        }
        else
        {
            // Invoke directly on the IO thread.
            Packet msg = new(buffer, this);
            OnMessage(msg);
        }
    }

    /// <summary>
    /// Starts asynchronous send.
    /// </summary>
    private void StartSend()
    {
        try
        {
            // Switched to using BufferList in SetBuffer for better performance.
            SendEventArgs.BufferList = _sendingList;

            // Start asynchronous send.
            var pending = Socket.SendAsync(SendEventArgs);
            if (!pending)
            {
                ProcessSend(SendEventArgs);
            }
        }
        catch (Exception e)
        {
            if (Socket is null)
            {
                Close();
                return;
            }

            Console.WriteLine("send error!! close socket. " + e.Message);
            throw new Exception(e.Message, e);
        }
    }
}
