using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	using FreeNet;
	using System.Threading;

	class CGameServer
	{
        // 게임방을 관리하는 매니저.
        public CGameRoomManager room_manager { get; private set; }

        // 매칭 대기 리스트.
        List<CGameUser> matching_waiting_users;
        //----------------------------------------------------------------

		public CGameServer()
		{
            room_manager = new CGameRoomManager();
            matching_waiting_users = new List<CGameUser>();
		}


        /// <summary>
        /// 유저로부터 매칭 요청이 왔을 때 호출됨.
        /// </summary>
        /// <param name="user">매칭을 신청한 유저 객체</param>
        public void matching_req(CGameUser user)
        {
            // 대기 리스트에 중복 추가 되지 않도록 체크.
			if (matching_waiting_users.Contains(user))
			{
				return;
			}

            // 매칭 대기 리스트에 추가.
            matching_waiting_users.Add(user);

            // 2명이 모이면 매칭 성공.
            if (matching_waiting_users.Count == 2)
            {
                // 게임 방 생성.
                room_manager.create_room(matching_waiting_users[0], matching_waiting_users[1]);

                // 매칭 대기 리스트 삭제.
                matching_waiting_users.Clear();
            }
            else
            {
                // 매칭 인원이 모자를 경우 대기 메시지 전송.
                Packet msg = Packet.Create((short)PROTOCOL.ENTER_GAME_ROOM_ACK);
                user.Send(msg);
            }
        }


        /// <summary>
        /// 유저가 끊겼을 경우 매칭 대기 리스트에서 제거.
        /// </summary>
        /// <param name="user"></param>
		public void user_disconnected(CGameUser user)
		{
			if (matching_waiting_users.Contains(user))
			{
				matching_waiting_users.Remove(user);
			}
		}
	}
}
