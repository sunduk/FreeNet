using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FreeNet
{
	/// <summary>
	/// 서버와 클라이언트에서 공통으로 사용하는 세션 객체.
	/// 서버일 경우 :
	///		하나의 클라이언트 객체를 나타낸다.
	///		이 인터페이스를 구현한 객체를 CNetworkService클래스의 session_created_callback호출시 생성하여 리턴시켜 준다.
	///		객체를 풀링할지 여부는 사용자가 원하는대로 구현한다.
	///	
	/// 클라이언트일 경우 :
	///		접속한 서버 객체를 나타낸다.
	///	
	/// </summary>
	public interface IPeer
	{
        // 제거됨.
        //void on_message(ArraySegment<byte> buffer);

        // 제거됨.
        //void process_user_operation(CPacket msg);


        /// <summary>
        /// CNetworkService.initialize에서 use_logicthread를 true로 설정할 경우
        /// -> IO스레드에서 직접 호출됨.
        /// 
        /// false로 설정할 경우
        /// -> 로직 스레드에서 호출됨. 로직 스레드는 싱글 스레드로 돌아감.
        /// </summary>
        /// <param name="buffer"></param>
        void on_message(CPacket msg);


        /// <summary>
        /// 원격 연결이 끊겼을 때 호출 된다.
        /// 이 매소드가 호출된 이후부터는 데이터 전송이 불가능하다.
        /// </summary>
        void on_removed();


		void send(CPacket msg);


		void disconnect();
    }
}
