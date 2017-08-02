using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    /// <summary>
    /// 현재 접속중인 전체 유저를 관리하는 클래스.
    /// </summary>
    public class CServerUserManager
    {
        object cs_user;
        List<CUserToken> users;


        public CServerUserManager()
        {
            this.cs_user = new object();
            this.users = new List<CUserToken>();
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
    }
}
