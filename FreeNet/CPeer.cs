using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
	/// <summary>
	/// 하나의 session객체를 나타낸다.
	/// CUserToken보다 로직쪽에 더 근접한 성격의 클래스이다.
	/// </summary>
	class CPeer
	{
		/// <summary>
		/// 소켓 버퍼로부터 데이터를 수신하여 패킷 하나를 완성했을 때 호출 된다.
		/// 호출 흐름 : .Net Socket ReceiveAsync -> CUserToken.on_receive -> CPeer.on_message
		/// 
		/// 패킷 순서에 대해서(TCP)
		///		이 매소드는 .Net Socket의 스레드풀에 의해 작동되어 호출되므로 어느 스레드에서 호출될지 알 수 없다.
		///		하지만 하나의 CPeer객체에 대해서는 이 매소드가 완료된 이후 다음 패킷이 들어오도록 구현되어 있으므로
		///		클라이언트가 보낸 패킷 순서는 보장이 된다.
		///		
		/// 주의할점
		///		이 매소드에서 다른 CPeer객체를 참조하거나 공유자원에 접근할 때는 멀티스레드 관련 문제가 발생할 수 있으므로
		///		lock등의 처리를 해줘야 한다.
		///		게임 패킷을 처리할 때는 lock을 걸고 queue에 복사한 뒤 싱글 스레드로 처리하는것이 편하다.
		/// </summary>
		/// <param name="buffer">
		/// Socket버퍼로부터 복사된 CUserToken의 버퍼를 참조한다.
		/// 이 매소드가 리턴되면 buffer는 비워지며 다음 패킷을 담을 준비를 한다.
		/// 따라서 매소드를 리턴하기 전에 사용할 데이터를 모두 빼내야 한다.
		/// </param>
		public void on_message(Const<byte[]> buffer)
		{
			CPacket msg = new CPacket(buffer.Value);
			Int16 protocol_id = msg.pop_int16();
			switch (protocol_id)
			{
				case 1:
					{
						Int32 number = msg.pop_int32();
						string text = msg.pop_string();

						Console.WriteLine(string.Format("[{0}] [received] {1} : {2}, {3}",
							System.Threading.Thread.CurrentThread.ManagedThreadId,
							protocol_id, number, text));
					}
					break;
			}
		}
	}
}
