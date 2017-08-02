using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    /// <summary>
    /// 두개의 큐를 교체해가며 활용한다.
    /// IO스레드에서 입력큐에 막 쌓아놓고,
    /// 로직스레드에서 큐를 뒤바꾼뒤(swap) 쌓아놓은 패킷을 가져가 처리한다.
    /// 참고 : http://roadster.egloos.com/m/4199854
    /// </summary>
    class CDoubleBufferingQueue : ILogicQueue
    {
        // 실제 데이터가 들어갈 큐.
        Queue<CPacket> queue1;
        Queue<CPacket> queue2;

        // 각각의 큐에 대한 참조.
        Queue<CPacket> ref_input;
        Queue<CPacket> ref_output;

        object cs_write;


        public CDoubleBufferingQueue()
        {
            // 초기 세팅은 큐와 참조가 1:1로 매칭되게 설정한다.
            // ref_input - queue1
            // ref)output - queue2
            this.queue1 = new Queue<CPacket>();
            this.queue2 = new Queue<CPacket>();
            this.ref_input = this.queue1;
            this.ref_output = this.queue2;

            this.cs_write = new object();
        }


        /// <summary>
        /// IO스레드에서 전달한 패킷을 보관한다.
        /// </summary>
        /// <param name="msg"></param>
        void ILogicQueue.enqueue(CPacket msg)
        {
            lock (this.cs_write)
            {
                this.ref_input.Enqueue(msg);
            }
        }


        Queue<CPacket> ILogicQueue.get_all()
        {
            swap();
            return this.ref_output;
        }


        /// <summary>
        /// 입력큐와 출력큐를 뒤바꾼다.
        /// </summary>
        void swap()
        {
            lock (this.cs_write)
            {
                Queue<CPacket> temp = this.ref_input;
                this.ref_input = this.ref_output;
                this.ref_output = temp;
            }
        }
    }
}
