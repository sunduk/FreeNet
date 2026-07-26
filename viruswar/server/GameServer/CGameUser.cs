using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer
{
    using UserState;

	/// <summary>
	/// 하나의 session객체를 나타낸다.
	/// </summary>
	public class CGameUser : IPeer
	{
		UserToken token;

		public CGameRoom battle_room { get; private set; }

		public CPlayer player { get; private set; }
        IUserState current_user_state;
        Dictionary<USER_STATE_TYPE, IUserState> user_states;

		public CGameUser(UserToken token)
		{
			this.token = token;
			this.token.SetPeer(this);

            user_states = new Dictionary<USER_STATE_TYPE, IUserState>();
            user_states.Add(USER_STATE_TYPE.LOBBY, new CUserLobbyState(this));
            user_states.Add(USER_STATE_TYPE.PLAY, new CUserPlayState(this));
            change_state(USER_STATE_TYPE.LOBBY);
        }

        public void change_state(USER_STATE_TYPE state)
        {
            current_user_state = user_states[state];
        }

        void IPeer.OnMessage(Packet msg)
		{
            switch ((PROTOCOL)msg.ProtocolId)
            {
                case PROTOCOL.CONCURRENT_USERS:
                    {
                        int count = Program.get_concurrent_user_count();
                        Packet reply = Packet.Create((short)PROTOCOL.CONCURRENT_USERS);
                        reply.Push(count);
                        Send(reply);
                    }
                    return;
            }

            current_user_state.on_message(msg);
        }

		void IPeer.OnRemoved()
		{
			Console.WriteLine("The client disconnected.");
            Program.remove_user(this);

            if (battle_room != null)
            {
                battle_room.on_player_removed(player);
            }
		}

		public void Send(Packet msg)
		{
            msg.RecordSize();

            // 소켓 버퍼로 보내기 전에 복사해 놓음.
            byte[] clone = new byte[msg.Position];
            Array.Copy(msg.Buffer, clone, msg.Position);

			token.Send(new ArraySegment<byte>(clone, 0, msg.Position));
		}

		void IPeer.Disconnect()
		{
            token.Ban();
		}

		public void enter_room(CGameRoom room, byte player_index)
		{
            player = new CPlayer(this, player_index);
			battle_room = room;
            change_state(USER_STATE_TYPE.PLAY);
		}
	}
}
