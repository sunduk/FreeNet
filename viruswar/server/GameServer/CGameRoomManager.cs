using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 게임방들을 관리하는 룸매니저.
    /// </summary>
    public class CGameRoomManager
    {
        List<CGameRoom> rooms;

        public CGameRoomManager()
        {
            this.rooms = new List<CGameRoom>();
        }


        /// <summary>
        /// 매칭을 요청한 유저들을 넘겨 받아 게임 방을 생성한다.
        /// </summary>
        /// <param name="user1"></param>
        /// <param name="user2"></param>
        public void create_room(CGameUser user1, CGameUser user2)
        {
            // 게임 방을 생성하여 입장 시킴.
            CGameRoom battleroom = new CGameRoom(this);
            this.rooms.Add(battleroom);

            user1.enter_room(battleroom, 0);
            user2.enter_room(battleroom, 1);

            battleroom.enter_gameroom(user1.player, user2.player);
        }

        public void remove_room(CGameRoom room)
        {
            room.destroy();
            this.rooms.Remove(room);
        }
    }
}
