using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
   	class Defines
	{
		public static readonly short HEADERSIZE = 2;
	}

	/// <summary>
	/// [header][body] 구조를 갖는 데이터를 파싱하는 클래스.
	/// - header : 데이터 사이즈. Defines.HEADERSIZE에 정의된 타입만큼의 크기를 갖는다.
	///				2바이트일 경우 Int16, 4바이트는 Int32로 처리하면 된다.
	///				본문의 크기가 Int16.Max값을 넘지 않는다면 2바이트로 처리하는것이 좋을것 같다.
	/// - body : 메시지 본문.
	/// </summary>
	class CMessageResolver
	{
		public delegate void CompletedMessageCallback(Const<byte[]> buffer);

		// 메시지 사이즈.
		int message_size;

		// 진행중인 버퍼.
		byte[] message_buffer = new byte[1024];

		// 현재 진행중인 버퍼의 인덱스를 가리키는 변수.
		// 패킷 하나를 완성한 뒤에는 0으로 초기화 시켜줘야 한다.
		int current_position;

		// 읽어와야 할 목표 위치.
		int position_to_read;

		// 남은 사이즈.
		int remain_bytes;

		public CMessageResolver()
		{
			this.message_size = 0;
			this.current_position = 0;
			this.position_to_read = 0;
			this.remain_bytes = 0;
		}

		/// <summary>
		/// 목표지점으로 설정된 위치까지의 바이트를 원본 버퍼로부터 복사한다.
		/// 데이터가 모자랄 경우 현재 남은 바이트 까지만 복사한다.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="transffered"></param>
		/// <param name="size_to_read"></param>
		/// <returns>다 읽었으면 true, 데이터가 모자라서 못 읽었으면 false를 리턴한다.</returns>
		bool read_until(byte[] buffer, ref int src_position, int offset, int transffered)
		{
			if (this.current_position >= offset + transffered)
			{
				// 들어온 데이터 만큼 다 읽은 상태이므로 더이상 읽을 데이터가 없다.
				return false;
			}

			// 읽어와야 할 바이트.
			// 데이터가 분리되어 올 경우 이전에 읽어놓은 값을 빼줘서 부족한 만큼 읽어올 수 있도록 계산해 준다.
			int copy_size = this.position_to_read - this.current_position;

			// 앗! 남은 데이터가 더 적다면 가능한 만큼만 복사한다.
			if (this.remain_bytes < copy_size)
			{
				copy_size = this.remain_bytes;
			}

			// 버퍼에 복사.
			Array.Copy(buffer, src_position, this.message_buffer, this.current_position, copy_size);


			// 원본 버퍼 포지션 이동.
			src_position += copy_size;

			// 타겟 버퍼 포지션도 이동.
			this.current_position += copy_size;

			// 남은 바이트 수.
			this.remain_bytes -= copy_size;

			// 목표지점에 도달 못했으면 false
			if (this.current_position < this.position_to_read)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 소켓 버퍼로부터 데이터를 수신할 때 마다 호출된다.
		/// 데이터가 남아 있을 때 까지 계속 패킷을 만들어 callback을 호출 해 준다.
		/// 하나의 패킷을 완성하지 못했다면 버퍼에 보관해 놓은 뒤 다음 수신을 기다린다.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="transffered"></param>
		public void on_receive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
		{
			// 이번 receive로 읽어오게 될 바이트 수.
			this.remain_bytes = transffered;

			// 원본 버퍼의 포지션값.
			// 패킷이 여러개 뭉쳐 올 경우 원본 버퍼의 포지션은 계속 앞으로 가야 하는데 그 처리를 위한 변수이다.
			int src_position = offset;

			// 남은 데이터가 있다면 계속 반복한다.
			while (this.remain_bytes > 0)
			{
				bool completed = false;

				// 헤더만큼 못읽은 경우 헤더를 먼저 읽는다.
				if (this.current_position < Defines.HEADERSIZE)
				{
					// 목표 지점 설정(헤더 위치까지 도달하도록 설정).
					this.position_to_read = Defines.HEADERSIZE;

					completed = read_until(buffer, ref src_position, offset, transffered);
					if (!completed)
					{
						// 아직 다 못읽었으므로 다음 receive를 기다린다.
						return;
					}

					// 헤더 하나를 온전히 읽어왔으므로 메시지 사이즈를 구한다.
					this.message_size = get_body_size();

					// 다음 목표 지점(헤더 + 메시지 사이즈).
					this.position_to_read = this.message_size + Defines.HEADERSIZE;
				}

				// 메시지를 읽는다.
				completed = read_until(buffer, ref src_position, offset, transffered);

				if (completed)
				{
					// 패킷 하나를 완성 했다.
					callback(new Const<byte[]>(this.message_buffer));

					clear_buffer();
				}
			}
		}

		int get_body_size()
		{
			// 헤더 타입의 바이트만큼을 읽어와 메시지 사이즈를 리턴한다.

			Type type = Defines.HEADERSIZE.GetType();
			if (type.Equals(typeof(Int16)))
			{
				return BitConverter.ToInt16(this.message_buffer, 0);
			}

			return BitConverter.ToInt32(this.message_buffer, 0);
		}

		void clear_buffer()
		{
			Array.Clear(this.message_buffer, 0, this.message_buffer.Length);

			this.current_position = 0;
			this.message_size = 0;

		}
	}
}
