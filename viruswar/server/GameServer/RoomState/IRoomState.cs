using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.RoomState
{
    using FreeNet;

    public interface IRoomState
    {
        void on_receive(PROTOCOL protocol, CPlayer owner, CPacket msg);
    }
}
