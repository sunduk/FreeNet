using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleServer
{
    using GameServer;

    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    class CGameUser : IPeer
    {
        UserToken token;

        public CGameUser(UserToken token)
        {
            this.token = token;
            this.token.SetPeer(this);
        }

        void IPeer.OnRemoved()
        {
            //Console.WriteLine("The client disconnected.");

            Program.remove_user(this);
        }

        public void Send(Packet msg)
        {
            msg.RecordSize();
            token.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
        }

        public void send(ArraySegment<byte> data)
        {
            token.Send(data);
        }

        void IPeer.Disconnect()
        {
            token.Ban();
        }

        void IPeer.OnMessage(Packet msg)
        {
            // 에코서버 테스트할 때 사용함.
            // Remove below comments to use echo server.
            //send(msg);
            //return;

            // ex)
            PROTOCOL protocol = (PROTOCOL)msg.PopProtocolId();
            //Console.WriteLine("------------------------------------------------------");
            //Console.WriteLine("protocol id " + protocol);
            switch (protocol)
            {
                case PROTOCOL.CHAT_MSG_REQ:
                    {
                        string text = msg.PopString();
                        Console.WriteLine(string.Format("text {0}", text));

                        Packet response = Packet.Create((short)PROTOCOL.CHAT_MSG_ACK);
                        response.Push(text);
                        Send(response);

                        if (text.Equals("exit"))
                        {
                            // 대량의 메시지를 한꺼번에 보낸 후 종료하는 시나리오 테스트.
                            for (int i = 0; i < 1000; ++i)
                            {
                                Packet dummy = Packet.Create((short)PROTOCOL.CHAT_MSG_ACK);
                                dummy.Push(i.ToString());
                                Send(dummy);
                            }

                            token.Ban();
                        }
                    }
                    break;
            }
        }
    }
}
