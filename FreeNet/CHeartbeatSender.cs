using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNet
{
    class CHeartbeatSender
    {
        CUserToken server;
        Timer timer_heartbeat;
        uint interval;

        float elapsed_time;


        public CHeartbeatSender(CUserToken server, uint interval)
        {
            this.server = server;
            this.interval = interval;
            this.timer_heartbeat = new Timer(this.on_timer, null, Timeout.Infinite, this.interval * 1000);
        }


        void on_timer(object state)
        {
            send();
        }


        void send()
        {
            CPacket msg = CPacket.create((short)CUserToken.SYS_UPDATE_HEARTBEAT);
            this.server.send(msg);
        }


        public void update(float time)
        {
            this.elapsed_time += time;
            if (this.elapsed_time < this.interval)
            {
                return;
            }

            this.elapsed_time = 0.0f;
            send();
        }


        public void stop()
        {
            this.elapsed_time = 0;
            this.timer_heartbeat.Change(Timeout.Infinite, Timeout.Infinite);
        }


        public void play()
        {
            this.elapsed_time = 0;
            this.timer_heartbeat.Change(0, this.interval * 1000);
        }
    }
}
