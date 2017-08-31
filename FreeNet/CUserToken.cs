using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace FreeNet
{
    public class CUserToken
    {
        enum State
        {
            // 대기중.
            Idle,

            // 연결됨.
            Connected,

            // 종료가 예약됨.
            // sending_list에 대기중인 상태에서 disconnect를 호출한 경우,
            // 남아있는 패킷을 모두 보낸 뒤 끊도록 하기 위한 상태값.
            ReserveClosing,

            // 소켓이 완전히 종료됨.
            Closed,
        }

        // 종료 요청. S -> C
        const short SYS_CLOSE_REQ = 0;
        // 종료 응답. C -> S
        const short SYS_CLOSE_ACK = -1;
        // 하트비트 시작. S -> C
        public const short SYS_START_HEARTBEAT = -2;
        // 하트비트 갱신. C -> S
        public const short SYS_UPDATE_HEARTBEAT = -3;

        // close중복 처리 방지를 위한 플래그.
        // 0 = 연결된 상태.
        // 1 = 종료된 상태.
        int is_closed;

        State current_state;
        public Socket socket { get; set; }

        public SocketAsyncEventArgs receive_event_args { get; private set; }
        public SocketAsyncEventArgs send_event_args { get; private set; }

        // 바이트를 패킷 형식으로 해석해주는 해석기.
        CMessageResolver message_resolver;

        // session객체. 어플리케이션 딴에서 구현하여 사용.
        IPeer peer;

        // BufferList적용을 위해 queue에서 list로 변경.
        List<ArraySegment<byte>> sending_list;
        // sending_list lock처리에 사용되는 객체.
        private object cs_sending_queue;

        IMessageDispatcher dispatcher;

        public delegate void ClosedDelegate(CUserToken token);
        public ClosedDelegate on_session_closed;

        // heartbeat.
        public long latest_heartbeat_time { get; private set; }
        CHeartbeatSender heartbeat_sender;
        bool auto_heartbeat;


        public CUserToken(IMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.cs_sending_queue = new object();

            this.message_resolver = new CMessageResolver();
            this.peer = null;
            this.sending_list = new List<ArraySegment<byte>>();
            this.latest_heartbeat_time = DateTime.Now.Ticks;

            this.current_state = State.Idle;
        }

        public void on_connected()
        {
            this.current_state = State.Connected;
            this.is_closed = 0;
            this.auto_heartbeat = true;
        }

        public void set_peer(IPeer peer)
        {
            this.peer = peer;
        }

        public void set_event_args(SocketAsyncEventArgs receive_event_args, SocketAsyncEventArgs send_event_args)
        {
            this.receive_event_args = receive_event_args;
            this.send_event_args = send_event_args;
        }

        /// <summary>
        ///	이 매소드에서 직접 바이트 데이터를 해석해도 되지만 Message resolver클래스를 따로 둔 이유는
        ///	추후에 확장성을 고려하여 다른 resolver를 구현할 때 CUserToken클래스의 코드 수정을 최소화 하기 위함이다.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="transfered"></param>
        public void on_receive(byte[] buffer, int offset, int transfered)
        {
            this.message_resolver.on_receive(buffer, offset, transfered, on_message_completed);
        }

        void on_message_completed(ArraySegment<byte> buffer)
        {
            if (this.peer == null)
            {
                return;
            }

            if (this.dispatcher != null)
            {
                // 로직 스레드의 큐를 타고 호출되도록 함.
                this.dispatcher.on_message(this, buffer);
            }
            else
            {
                // IO스레드에서 직접 호출.
                CPacket msg = new CPacket(buffer, this);
                on_message(msg);
            }
        }


        public void on_message(CPacket msg)
        {
            // active close를 위한 코딩.
            //   서버에서 종료하라고 연락이 왔는지 체크한다.
            //   만약 종료신호가 맞다면 disconnect를 호출하여 받은쪽에서 먼저 종료 요청을 보낸다.
            switch (msg.protocol_id)
            {
                case SYS_CLOSE_REQ:
                    disconnect();
                    return;

                case SYS_START_HEARTBEAT:
                    {
                        // 순서대로 파싱해야 하므로 프로토콜 아이디는 버린다.
                        msg.pop_protocol_id();
                        // 전송 인터벌.
                        byte interval = msg.pop_byte();
                        this.heartbeat_sender = new CHeartbeatSender(this, interval);

                        if (this.auto_heartbeat)
                        {
                            start_heartbeat();
                        }
                    }
                    return;

                case SYS_UPDATE_HEARTBEAT:
                    //Console.WriteLine("heartbeat : " + DateTime.Now);
                    this.latest_heartbeat_time = DateTime.Now.Ticks;
                    return;
            }


            if (this.peer != null)
            {
                try
                {
                    switch (msg.protocol_id)
                    {
                        case SYS_CLOSE_ACK:
                            this.peer.on_removed();
                            break;

                        default:
                            this.peer.on_message(msg);
                            break;
                    }
                }
                catch (Exception)
                {
                    close();
                }
            }

            if (msg.protocol_id == SYS_CLOSE_ACK)
            {
                if (this.on_session_closed != null)
                {
                    this.on_session_closed(this);
                }
            }
        }

        public void close()
        {
            // 중복 수행을 막는다.
            if (Interlocked.CompareExchange(ref this.is_closed, 1, 0) == 1)
            {
                return;
            }

            if (this.current_state == State.Closed)
            {
                // already closed.
                return;
            }

            this.current_state = State.Closed;
            this.socket.Close();
            this.socket = null;

            this.send_event_args.UserToken = null;
            this.receive_event_args.UserToken = null;

            this.sending_list.Clear();
            this.message_resolver.clear_buffer();

            if (this.peer != null)
            {
                CPacket msg = CPacket.create((short)-1);
                if (this.dispatcher != null)
                {
                    this.dispatcher.on_message(this, new ArraySegment<byte>(msg.buffer, 0, msg.position));
                }
                else
                {
                    on_message(msg);
                }
            }
        }


        /// <summary>
        /// 패킷을 전송한다.
        /// 큐가 비어 있을 경우에는 큐에 추가한 뒤 바로 SendAsync매소드를 호출하고,
        /// 데이터가 들어있을 경우에는 새로 추가만 한다.
        /// 
        /// 큐잉된 패킷의 전송 시점 :
        ///		현재 진행중인 SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송한다.
        /// </summary>
        /// <param name="msg"></param>
        public void send(ArraySegment<byte> data)
        {
            lock (this.cs_sending_queue)
            {
                this.sending_list.Add(data);

                if (this.sending_list.Count > 1)
                {
                    // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴한다.
                    // 현재 수행중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
                    return;
                }
            }

            start_send();
        }


        public void send(CPacket msg)
        {
            msg.record_size();
            send(new ArraySegment<byte>(msg.buffer, 0, msg.position));
        }


        /// <summary>
        /// 비동기 전송을 시작한다.
        /// </summary>
        void start_send()
        {
            try
            {
                // 성능 향상을 위해 SetBuffer에서 BufferList를 사용하는 방식으로 변경함.
                this.send_event_args.BufferList = this.sending_list;

                // 비동기 전송 시작.
                bool pending = this.socket.SendAsync(this.send_event_args);
                if (!pending)
                {
                    process_send(this.send_event_args);
                }
            }
            catch (Exception e)
            {
                if (this.socket == null)
                {
                    close();
                    return;
                }

                Console.WriteLine("send error!! close socket. " + e.Message);
                throw new Exception(e.Message, e);
            }
        }

        static int sent_count = 0;
        static object cs_count = new object();
        /// <summary>
        /// 비동기 전송 완료시 호출되는 콜백 매소드.
        /// </summary>
        /// <param name="e"></param>
        public void process_send(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                // 연결이 끊겨서 이미 소켓이 종료된 경우일 것이다.
                //Console.WriteLine(string.Format("Failed to send. error {0}, transferred {1}", e.SocketError, e.BytesTransferred));
                return;
            }

            lock (this.cs_sending_queue)
            {
                // 리스트에 들어있는 데이터의 총 바이트 수.
                var size = this.sending_list.Sum(obj => obj.Count);

                // 전송이 완료되기 전에 추가 전송 요청을 했다면 sending_list에 무언가 더 들어있을 것이다.
                if (e.BytesTransferred != size)
                {
                    //todo:세그먼트 하나를 다 못보낸 경우에 대한 처리도 해줘야 함.
                    // 일단 close시킴.
                    if (e.BytesTransferred < this.sending_list[0].Count)
                    {
                        string error = string.Format("Need to send more! transferred {0},  packet size {1}", e.BytesTransferred, size);
                        Console.WriteLine(error);

                        close();
                        return;
                    }

                    // 보낸 만큼 빼고 나머지 대기중인 데이터들을 한방에 보내버린다.
                    int sent_index = 0;
                    int sum = 0;
                    for (int i = 0; i < this.sending_list.Count; ++i)
                    {
                        sum += this.sending_list[i].Count;
                        if (sum <= e.BytesTransferred)
                        {
                            // 여기 까지는 전송 완료된 데이터 인덱스.
                            sent_index = i;
                            continue;
                        }

                        break;
                    }
                    // 전송 완료된것은 리스트에서 삭제한다.
                    this.sending_list.RemoveRange(0, sent_index + 1);

                    // 나머지 데이터들을 한방에 보낸다.
                    start_send();
                    return;
                }

                // 다 보냈고 더이상 보낼것도 없다.
                this.sending_list.Clear();
 
                // 종료가 예약된 경우, 보낼건 다 보냈으니 진짜 종료 처리를 진행한다.
                if (this.current_state == State.ReserveClosing)
                {
                    this.socket.Shutdown(SocketShutdown.Send);
                }
            }
        }


        /// <summary>
        /// 연결을 종료한다.
        /// 주로 클라이언트에서 종료할 때 호출한다.
        /// </summary>
        public void disconnect()
        {
            // close the socket associated with the client
            try
            {
                if (this.sending_list.Count <= 0)
                {
                    this.socket.Shutdown(SocketShutdown.Send);
                    return;
                }

                this.current_state = State.ReserveClosing;
            }
            // throws if client process has already closed
            catch (Exception)
            {
                close();
            }
        }


        /// <summary>
        /// 연결을 종료한다. 단, 종료코드를 전송한 뒤 상대방이 먼저 연결을 끊게 한다.
        /// 주로 서버에서 클라이언트의 연결을 끊을 때 사용한다.
        /// 
        /// TIME_WAIT상태를 서버에 남기지 않으려면 disconnect대신 이 매소드를 사용해서
        /// 클라이언트를 종료시켜야 한다.
        /// </summary>
        public void ban()
        {
            try
            {
                byebye();
            }
            catch (Exception)
            {
                close();
            }
        }


        /// <summary>
        /// 종료코드를 전송하여 상대방이 먼저 끊도록 한다.
        /// </summary>
        void byebye()
        {
            CPacket bye = CPacket.create(SYS_CLOSE_REQ);
            send(bye);
        }


        public bool is_connected()
        {
            return this.current_state == State.Connected;
        }


        public void start_heartbeat()
        {
            if (this.heartbeat_sender != null)
            {
                this.heartbeat_sender.play();
            }
        }


        public void stop_heartbeat()
        {
            if (this.heartbeat_sender != null)
            {
                this.heartbeat_sender.stop();
            }
        }


        public void disable_auto_heartbeat()
        {
            stop_heartbeat();
            this.auto_heartbeat = false;
        }


        public void update_heartbeat_manually(float time)
        {
            if (this.heartbeat_sender != null)
            {
                this.heartbeat_sender.update(time);
            }
        }
    }
}
