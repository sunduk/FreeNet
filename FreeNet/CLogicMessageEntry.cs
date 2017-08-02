using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNet
{
    /// <summary>
    /// 수신된 패킷을 받아 로직 스레드에서 분배하는 역할을 담당한다.
    /// </summary>
    public class CLogicMessageEntry : IMessageDispatcher
    {
        CNetworkService service;
        ILogicQueue message_queue;
        AutoResetEvent logic_event;


        public CLogicMessageEntry(CNetworkService service)
        {
            this.service = service;
            this.message_queue = new CDoubleBufferingQueue();
            this.logic_event = new AutoResetEvent(false);
        }


        /// <summary>
        /// 로직 스레드 시작.
        /// </summary>
        public void start()
        {
            Thread logic = new Thread(this.do_logic);
            logic.Start();
        }


        void IMessageDispatcher.on_message(CUserToken user, ArraySegment<byte> buffer)
        {
            // 여긴 IO스레드에서 호출된다.
            // 완성된 패킷을 메시지큐에 넣어준다.
            CPacket msg = new CPacket(buffer, user);
            this.message_queue.enqueue(msg);

            // 로직 스레드를 깨워 일을 시킨다.
            this.logic_event.Set();
        }


        /// <summary>
        /// 로직 스레드.
        /// </summary>
        void do_logic()
        {
            while (true)
            {
                // 패킷이 들어오면 알아서 깨워 주겠지.
                this.logic_event.WaitOne();

                // 메시지를 분배한다.
                dispatch_all(this.message_queue.get_all());
            }
        }


        void dispatch_all(Queue<CPacket> queue)
        {
            while (queue.Count > 0)
            {
                CPacket msg = queue.Dequeue();
                if (!this.service.usermanager.is_exist(msg.owner))
                {
                    continue;
                }

                msg.owner.on_message(msg);
            }
        }
    }
}
