using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    public interface IMessageDispatcher
    {
        void on_message(IPeer user, ArraySegment<byte> buffer);
    }
}
