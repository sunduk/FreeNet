using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleClient
{
    using GameServer;

    class CRemoteServerPeer : IPeer
    {
        public UserToken token { get; private set; }

        public CRemoteServerPeer(UserToken token)
        {
            this.token = token;
            this.token.SetPeer(this);
        }

        int recv_count = 0;
        void IPeer.OnMessage(Packet msg)
        {
            System.Threading.Interlocked.Increment(ref recv_count);

            PROTOCOL protocol_id = (PROTOCOL)msg.PopProtocolId();
            switch (protocol_id)
            {
                case PROTOCOL.CHAT_MSG_ACK:
                    {
                        string text = msg.PopString();
                        Console.WriteLine(string.Format("text {0}", text));
                    }
                    break;
            }
        }

        void IPeer.OnRemoved()
        {
            Console.WriteLine("Server removed.");
            Console.WriteLine("recv count " + recv_count);
        }

        void IPeer.Send(Packet msg)
        {
            msg.RecordSize();
            token.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
        }

        void IPeer.Disconnect()
        {
            token.Disconnect();
        }
    }
}
