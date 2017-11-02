using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNet
{
    /// <summary>
    /// 현재 접속중인 전체 유저를 관리하는 클래스.
    /// </summary>
    public class CServerUserManager
    {
        object cs_user;
        List<CUserToken> users;

        Timer timer_heartbeat;
        long heartbeat_duration;


        public CServerUserManager()
        {
            this.cs_user = new object();
            this.users = new List<CUserToken>();
        }


        public void start_heartbeat_checking(uint check_interval_sec, uint allow_duration_sec)
        {
            this.heartbeat_duration = allow_duration_sec * 10000000;
            this.timer_heartbeat = new Timer(check_heartbeat, null, 1000 * check_interval_sec, 1000 * check_interval_sec);
        }


        public void stop_heartbeat_checking()
        {
            this.timer_heartbeat.Dispose();
        }


        public void add(CUserToken user)
        {
            lock (this.cs_user)
            {
                this.users.Add(user);
            }
        }


        public void remove(CUserToken user)
        {
            lock (this.cs_user)
            {
                this.users.Remove(user);
            }
        }


        public bool is_exist(CUserToken user)
        {
            lock (this.cs_user)
            {
                return this.users.Exists(obj => obj == user);
            }
        }


        public int get_total_count()
        {
            return this.users.Count;
        }


        void check_heartbeat(object state)
        {
            long allowed_time = DateTime.Now.Ticks - this.heartbeat_duration;

            lock (this.cs_user)
            {
                for (int i = 0; i < this.users.Count; ++i)
                {
                    long heartbeat_time = this.users[i].latest_heartbeat_time;
                    if (heartbeat_time >= allowed_time)
                    {
                        continue;
                    }

                    this.users[i].disconnect();
                }
            }
        }


    }
}
