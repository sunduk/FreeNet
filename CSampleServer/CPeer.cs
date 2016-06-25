using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleServer
{
	/// <summary>
	/// 하나의 session객체를 나타낸다.
	/// </summary>
	class CPeer : IPeer
	{
		void IPeer.on_message(Const<byte[]> buffer)
		{
			// ex)
			CPacket msg = new CPacket(buffer.Value);
			Int16 protocol = msg.pop_protocol_id();
			Console.WriteLine("protocol id " + protocol);
			switch (protocol)
			{
				case 1:
					{
						Int32 age = msg.pop_int32();
						string text = msg.pop_string();
						Console.WriteLine(string.Format("age {0}, text {1}", age, text));
					}
					break;
			}
		}

		void IPeer.on_removed()
		{
			Console.WriteLine("The client disconnected.");

			// 오브젝트 풀링을 사용하고 있다면 여기서 반환 처리를 해줍니다.
			//...
		}
	}
}
