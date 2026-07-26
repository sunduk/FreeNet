using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer
{
	class Program
	{
		static List<CGameUser> userlist;
		public static CGameServer game_main = new CGameServer();

		static void Main(string[] args)
		{
			userlist = new List<CGameUser>();

			NetworkService service = new NetworkService(true);
			// 콜백 매소드 설정.
			service.SessionCreatedCallback += on_session_created;
			// 초기화.
			service.Initialize(10000, 1024);
			service.Listen("0.0.0.0", 20000, 100);


            Console.WriteLine("Started!");
			while (true)
			{
				string input = Console.ReadLine();
				//Console.Write(".");
				System.Threading.Thread.Sleep(1000);
			}
		}


		static void on_session_created(UserToken token)
		{
			CGameUser user = new CGameUser(token);
			lock (userlist)
			{
				userlist.Add(user);
			}
		}


		public static void remove_user(CGameUser user)
		{
			lock (userlist)
			{
				userlist.Remove(user);
				game_main.user_disconnected(user);
            }
		}


        public static int get_concurrent_user_count()
        {
            return userlist.Count;
        }
	}
}
