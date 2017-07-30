using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    public interface ILogicQueue
    {
        void enqueue(CPacket msg);
        void dispatch_all();
    }
}
