using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.UserState
{
    using FreeNet;

    public enum USER_STATE_TYPE
    {
        LOBBY,
        PLAY
    }

    interface IUserState
    {
        void on_message(CPacket msg);
    }
}
