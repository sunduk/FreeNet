using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    public static class Defines
    {
        public static readonly short HEADERSIZE = 2;
        public static readonly int MAXPAYLOAD = 4 * 1024;
        public static readonly int MAXMESSAGE = HEADERSIZE + MAXPAYLOAD;
    }

    public class CMessageResolver
    {
		// 메시지 사이즈.
        short message_size;

		// 진행중인 버퍼.
        byte[] message_buffer = new byte[SimpleBinaryMessageProtocol.MAXMESSAGE];

		int position = 0;

		void readn(byte[] buffer, int offset, int size_to_read)
		{
			// 버퍼에 새로 들어온 데이터 전부를 패킷 임시 버퍼에 복사한다.
			Array.Copy(buffer, offset, this.message_buffer, offset, size_to_read);
			this.position += size_to_read;
		}

		/// <summary>
		/// 바이트 수 만큼을 읽는다.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="transffered"></param>
		/// <param name="size_to_read"></param>
		/// <returns>다 읽었으면 true, 데이터가 모자라서 못 읽었으면 false를 리턴한다.</returns>
		bool read_until(byte[] buffer, int offset, int transffered, int size_to_read)
		{
			return true;
		}

		public void on_receive(byte[] buffer, int offset, int transffered)
        {
			int remain_bytes = this.position + transffered;

			// 남은 데이터가 있다면 계속 반복한다.
			while (remain_bytes > 0)
			{
				bool completed = false;
				// 헤더를 읽는다.
				if (this.position <= 0)
				{
					completed = read_until(buffer, offset, transffered, Defines.HEADERSIZE);
					if (!completed)
					{
						// 아직 다 못읽었으므로 다음 receive를 기다린다.
						return;
					}

					remain_bytes -= Defines.HEADERSIZE;

					// 헤더 하나를 온전히 읽어왔으므로 메시지 사이즈를 구한다.
					this.message_size = get_body_size();
				}

				// 메시지를 읽는다.
				completed = read_until(buffer, offset, transffered, this.message_size);

				if (completed)
				{
					// 패킷 하나를 처리한다.

					// 위치값 초기화.
					this.position = 0;
				}
			}


			// 헤더 읽기.
			//if (this.position < Defines.HEADERSIZE)
			//{
			//	// 헤더 크기 만큼이 다 왔는지 기다린 후 모자라다면 기다리고
			//	// 충분하다면 헤더만큼을 한번에 복사해온다.
			//	int current_readsize = this.position + transffered;
			//	if (current_readsize < Defines.HEADERSIZE)
			//	{
			//		// 헤더가 짤려서 왔으니 다음 receive를 기다린다.
			//		return;
			//	}

			//	// 헤더만큼을 읽는다.
			//	readn(buffer, 0, Defines.HEADERSIZE);

			//	// 헤더 하나를 온전히 읽어왔으므로 메시지 사이즈를 구한다.
			//	this.message_size = get_body_size();
			//}

			// 헤더 이후의 데이터가 얼마나 왔는지 체크하여
			//		하나도 없는 경우,
			//		메시지 사이즈보다 작은 경우,
			//		메시지 사이즈보다 큰 경우
			// 를 나눠서 처리한다.

			// 메시지 읽기.
			//readn(buffer, this.position, this.message_size);


            byte[] srcBuffer = arraySegment.Array;
            int srcEndIdx = arraySegment.Offset + arraySegment.Count;
            for (int srcIdx = arraySegment.Offset; srcIdx < srcEndIdx; ++srcIdx)
            {
                // 메세지 포인터가 범위를 넘어가면 안된다.
                if( _messagePos >= SimpleBinaryMessageProtocol.MAXMESSAGE )
                {
                    context.CloseMessageContext(CloseReason.MessageResolveError);
                    return true;
                }

                // 버퍼에 복사해 넣는다.
                _messageBuffer[_messagePos] = srcBuffer[srcIdx];
                ++_messagePos;

                // 메세지 size 를 구한다.
                if (_messageSize == 0 && _messagePos >= SimpleBinaryMessageProtocol.HEADERSIZE)
                {
                    _messageSize = GetPayloadLength() + SimpleBinaryMessageProtocol.HEADERSIZE;
                    if (_messageSize <= 0 || _messageSize >= SimpleBinaryMessageProtocol.MAXMESSAGE)
                    {
                        context.CloseMessageContext(CloseReason.MessageResolveError);
                        return true;
                    }
                }

                // 패킷이 완성되었으면 처리
                if (_messageSize != 0 && _messagePos == _messageSize)
                {
                    context.CompletedMessage(new ArraySegment<byte>(_messageBuffer, SimpleBinaryMessageProtocol.HEADERSIZE, _messageSize - SimpleBinaryMessageProtocol.HEADERSIZE));
                    ResetMessageBuffer();
                    continue;
                }
            }

            return true;
        }
       
        short get_body_size()
        {
            return BitConverter.ToInt16(this.message_buffer, 0);
        }

        private void ResetMessageBuffer()
        {
            _messagePos = 0;
            _messageSize = 0;
        }
    }
}
