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

        /// <summary>
        /// IO스레드에서 호출되는 매소드.
        /// 
        /// Called from IO thread.
        /// </summary>
        /// <param name="buffer"></param>
		void IPeer.on_message(ArraySegment<byte> buffer)
		{
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

        /// <summary>
        /// 로직 스레드에서 호출되는 매소드.
        /// 
        /// Called from logic thread(single thread).
        /// </summary>
        /// <param name="msg"></param>
		void IPeer.process_user_operation(CPacket msg)
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
                    }
                    break;
            }
        }
	}
}
