using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer.UserState
{
    class CUserLobbyState : IUserState
    {
        CGameUser owner;

        public CUserLobbyState(CGameUser owner)
        {
            this.owner = owner;
        }

        void IUserState.on_message(FreeNet.CPacket msg)
        {
            PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();
            Console.WriteLine("protocol id " + protocol);
            switch (protocol)
            {
                case PROTOCOL.ENTER_GAME_ROOM_REQ:
                    Program.game_main.matching_req(this.owner);
                    break;
            }
        }
    }
}
