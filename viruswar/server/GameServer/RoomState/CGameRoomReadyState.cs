using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.RoomState
{
    using FreeNet;

    class CGameRoomReadyState : IState
    {
        CGameRoom room;


        public CGameRoomReadyState(CGameRoom room)
        {
            this.room = room;

            this.room.state_manager.register_message_handler(this, PROTOCOL.READY_TO_START, this.on_ready_req);
        }


        void IState.on_enter()
        {
        }


        void IState.on_exit()
        {
        }


        void on_ready_req(CPlayer sender, CPacket msg)
        {
            if (!this.room.all_received(PROTOCOL.READY_TO_START))
            {
                return;
            }

            this.room.state_manager.change_state(CGameRoom.STATE.PLAY);
        }
    }
}
