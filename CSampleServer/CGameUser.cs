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
		CUserToken token;

		public CGameUser(CUserToken token)
		{
			this.token = token;
			this.token.set_peer(this);
		}

		void IPeer.on_removed()
		{
			//Console.WriteLine("The client disconnected.");

			Program.remove_user(this);
		}

		public void send(CPacket msg)
		{
            msg.record_size();
            this.token.send(new ArraySegment<byte>(msg.buffer, 0, msg.position));
		}

        public void send(ArraySegment<byte> data)
        {
            this.token.send(data);
        }

		void IPeer.disconnect()
		{
			this.token.socket.Disconnect(false);
		}

		void IPeer.on_message(CPacket msg)
		{
            // ex)
            PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();
            //Console.WriteLine("------------------------------------------------------");
            //Console.WriteLine("protocol id " + protocol);
            switch (protocol)
            {
                case PROTOCOL.CHAT_MSG_REQ:
                    {
                        string text = msg.pop_string();
                        Console.WriteLine(string.Format("text {0}", text));

                        CPacket response = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
                        response.push(text);
                        send(response);

                        if (text.Equals("exit"))
                        {
                            //todo:테스트.
                            //for (int i = 0; i < 1000; ++i)
                            //{
                            //    CPacket dummy = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
                            //    dummy.push(i.ToString());
                            //    send(dummy);
                            //    //System.Threading.Thread.Sleep(1);
                            //}

                            this.token.disconnect();
                        }
                    }
                    break;
            }
        }
	}
}
