using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.UserState
{
    class CUserPlayState : IUserState
    {
        CGameUser owner;

        public CUserPlayState(CGameUser owner)
        {
            this.owner = owner;
        }


        void IUserState.on_message(FreeNet.CPacket msg)
        {
            // 플레이중 수신된 모든 메시지는 룸으로 넘겨서 처리한다.
            this.owner.battle_room.on_receive(this.owner.player, msg);
        }
    }
}
