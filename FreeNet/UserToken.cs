using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace FreeNet;

/// <summary>
/// 사용자 연결을 나타내는 클래스.
/// </summary>
public class UserToken(IMessageDispatcher dispatcher)
{
    /// <summary>
    /// 하트비트 시작. S -&gt; C
    /// </summary>
    public const short SYS_START_HEARTBEAT = -2;

    /// <summary>
    /// 하트비트 갱신. C -&gt; S
    /// </summary>
    public const short SYS_UPDATE_HEARTBEAT = -3;

    /// <summary>
    /// 세션 종료 이벤트. 세션이 종료될 때 호출되는 콜백 매소드.
    /// </summary>
    public ClosedDelegate OnSessionClosed;

    /// <summary>
    /// 종료 응답. C -&gt; S
    /// </summary>
    private const short SYS_CLOSE_ACK = -1;

    /// <summary>
    /// 종료 요청. S -&gt; C
    /// </summary>
    private const short SYS_CLOSE_REQ = 0;

    /// <summary>
    /// 바이트를 패킷 형식으로 해석해주는 해석기.
    /// </summary>
    private readonly MessageResolver _messageResolver = new();

    /// <summary>
    /// BufferList적용을 위해 queue에서 list로 변경.
    /// </summary>
    private readonly List<ArraySegment<byte>> _sendingList = [];

    /// <summary>
    /// sending_list lock처리에 사용되는 객체.
    /// </summary>
    private readonly Lock _sendingQueueLock = new();

    private bool _autoHeartbeat;
    private State _currentState = State.Idle;
    private HeartbeatSender _heartbeatSender;

    /// <summary>
    /// close중복 처리 방지를 위한 플래그. 0 = 연결된 상태. 1 = 종료된 상태.
    /// </summary>
    private int _isClosed;

    /// <summary>
    /// session객체. 어플리케이션 딴에서 구현하여 사용.
    /// </summary>
    private IPeer _peer = null;

    public delegate void ClosedDelegate(UserToken token);

    /// <summary>
    /// 현재 연결 상태를 나타내는 열거형.
    /// </summary>
    private enum State
    {
        /// <summary>
        /// 대기중.
        /// </summary>
        Idle,

        /// <summary>
        /// 연결됨.
        /// </summary>
        Connected,

        /// <summary>
        /// 종료가 예약됨. sending_list에 대기중인 상태에서 disconnect를 호출한 경우, 남아있는 패킷을 모두 보낸 뒤 끊도록 하기 위한 상태값.
        /// </summary>
        ReserveClosing,

        /// <summary>
        /// 소켓이 완전히 종료됨.
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
    /// 연결을 종료한다. 단, 종료코드를 전송한 뒤 상대방이 먼저 연결을 끊게 한다. 주로 서버에서 클라이언트의 연결을 끊을 때 사용한다. TIME_WAIT상태를 서버에 남기지 않으려면 disconnect대신
    /// 이 매소드를 사용해서 클라이언트를 종료시켜야 한다.
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
        // 중복 수행을 막는다.
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

        if (SendEventArgs is not null)
        {
            SendEventArgs.UserToken = null;
        }

        if (ReceiveEventArgs is not null)
        {
            ReceiveEventArgs.UserToken = null;
        }

        _sendingList.Clear();
        _messageResolver.ClearBuffer();

        if (_peer is not null)
        {
            Packet msg = Packet.Create(-1);
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
    /// 연결을 종료한다. 주로 클라이언트에서 종료할 때 호출한다.
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

    public bool IsConnected()
    {
        return _currentState == State.Connected;
    }

    public void OnConnected()
    {
        _currentState = State.Connected;
        _isClosed = 0;
        _autoHeartbeat = true;
    }

    public void OnMessage(Packet msg)
    {
        // active close를 위한 코딩. 서버에서 종료하라고 연락이 왔는지 체크한다. 만약 종료신호가 맞다면 Disconnect를 호출하여 받은쪽에서 먼저 종료
        // 요청을 보낸다.
        switch (msg.ProtocolId)
        {
            case SYS_CLOSE_REQ:
                Disconnect();
                return;

            case SYS_START_HEARTBEAT:
                {
                    // 순서대로 파싱해야 하므로 프로토콜 아이디는 버린다.
                    _ = msg.PopProtocolId();
                    // 전송 인터벌.
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
    /// 이 매소드에서 직접 바이트 데이터를 해석해도 되지만 Message resolver클래스를 따로 둔 이유는 추후에 확장성을 고려하여 다른 resolver를 구현할 때 CUserToken클래스의 코드
    /// 수정을 최소화 하기 위함이다.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="transfered"></param>
    public void OnReceive(byte[] buffer, int offset, int transfered)
    {
        _messageResolver.OnReceive(buffer, offset, transfered, OnMessageCompleted);
    }

    /// <summary>
    /// 비동기 전송 완료시 호출되는 콜백 매소드.
    /// </summary>
    /// <param name="e"></param>
    public void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
        {
            // 전송 실패 시 세션을 명시적으로 종료해 반쯤 열린 상태를 방지한다.
            Close();
            return;
        }

        lock (_sendingQueueLock)
        {
            // 리스트에 들어있는 데이터의 총 바이트 수.
            var size = _sendingList.Sum(obj => obj.Count);

            // 전송이 완료되기 전에 추가 전송 요청을 했다면 sending_list에 무언가 더 들어있을 것이다.
            if (e.BytesTransferred != size)
            {
                // TODO: 세그먼트 하나를 다 못보낸 경우에 대한 처리도 해줘야 함. 일단 close시킴.
                if (e.BytesTransferred < _sendingList[0].Count)
                {
                    var error = string.Format("Need to send more! transferred {0},  packet size {1}", e.BytesTransferred, size);
                    Console.WriteLine(error);

                    Close();
                    return;
                }

                // 보낸 만큼 빼고 나머지 대기중인 데이터들을 한방에 보내버린다.
                var sent_index = 0;
                var sum = 0;
                for (var i = 0; i < _sendingList.Count; ++i)
                {
                    sum += _sendingList[i].Count;
                    if (sum <= e.BytesTransferred)
                    {
                        // 여기 까지는 전송 완료된 데이터 인덱스.
                        sent_index = i;
                        continue;
                    }

                    break;
                }
                // 전송 완료된것은 리스트에서 삭제한다.
                _sendingList.RemoveRange(0, sent_index + 1);

                // 나머지 데이터들을 한방에 보낸다.
                StartSend();
                return;
            }

            // 다 보냈고 더이상 보낼것도 없다.
            _sendingList.Clear();

            // 종료가 예약된 경우, 보낼건 다 보냈으니 진짜 종료 처리를 진행한다.
            if (_currentState == State.ReserveClosing)
            {
                Socket.Shutdown(SocketShutdown.Send);
            }
        }
    }

    /// <summary>
    /// 패킷을 전송한다. 큐가 비어 있을 경우에는 큐에 추가한 뒤 바로 SendAsync매소드를 호출하고, 데이터가 들어있을 경우에는 새로 추가만 한다. 큐잉된 패킷의 전송 시점: 현재 진행중인
    /// SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송한다.
    /// </summary>
    /// <param name="msg"></param>
    public void Send(ArraySegment<byte> data)
    {
        lock (_sendingQueueLock)
        {
            _sendingList.Add(data);

            if (_sendingList.Count > 1)
            {
                // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴한다. 현재 수행중인 SendAsync가 완료된 이후에
                // 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
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

    public void SetPeer(IPeer peer)
    {
        _peer = peer;
    }

    public void StartHeartbeat()
    {
        _heartbeatSender?.Play();
    }

    public void StopHeartbeat()
    {
        _heartbeatSender?.Stop();
    }

    public void UpdateHeartbeatManually(float time)
    {
        _heartbeatSender?.Update(time);
    }

    /// <summary>
    /// 종료코드를 전송하여 상대방이 먼저 끊도록 한다.
    /// </summary>
    private void ByeBye()
    {
        Packet bye = Packet.Create(SYS_CLOSE_REQ);
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
            // 로직 스레드의 큐를 타고 호출되도록 함.
            dispatcher.OnMessage(this, buffer);
        }
        else
        {
            // IO스레드에서 직접 호출.
            Packet msg = new(buffer, this);
            OnMessage(msg);
        }
    }

    /// <summary>
    /// 비동기 전송을 시작한다.
    /// </summary>
    private void StartSend()
    {
        try
        {
            // 성능 향상을 위해 SetBuffer에서 BufferList를 사용하는 방식으로 변경함.
            SendEventArgs.BufferList = _sendingList;

            // 비동기 전송 시작.
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