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
		CUserToken token;

		public CGameRoom battle_room { get; private set; }

		public CPlayer player { get; private set; }
        IUserState current_user_state;
        Dictionary<USER_STATE_TYPE, IUserState> user_states;

		public CGameUser(CUserToken token)
		{
			this.token = token;
			this.token.set_peer(this);

            this.user_states = new Dictionary<USER_STATE_TYPE, IUserState>();
            this.user_states.Add(USER_STATE_TYPE.LOBBY, new CUserLobbyState(this));
            this.user_states.Add(USER_STATE_TYPE.PLAY, new CUserPlayState(this));
            change_state(USER_STATE_TYPE.LOBBY);
        }

        public void change_state(USER_STATE_TYPE state)
        {
            this.current_user_state = this.user_states[state];
        }

        void IPeer.on_message(CPacket msg)
		{
            switch ((PROTOCOL)msg.protocol_id)
            {
                case PROTOCOL.CONCURRENT_USERS:
                    {
                        int count = Program.get_concurrent_user_count();
                        CPacket reply = CPacket.create((short)PROTOCOL.CONCURRENT_USERS);
                        reply.push(count);
                        send(reply);
                    }
                    return;
            }

            this.current_user_state.on_message(msg);
        }

		void IPeer.on_removed()
		{
			Console.WriteLine("The client disconnected.");
            Program.remove_user(this);

            if (this.battle_room != null)
            {
                this.battle_room.on_player_removed(this.player);
            }
		}

		public void send(CPacket msg)
		{
            msg.record_size();

            // 소켓 버퍼로 보내기 전에 복사해 놓음.
            byte[] clone = new byte[msg.position];
            Array.Copy(msg.buffer, clone, msg.position);

			this.token.send(new ArraySegment<byte>(clone, 0, msg.position));
		}

		void IPeer.disconnect()
		{
            this.token.ban();
		}

		public void enter_room(CGameRoom room, byte player_index)
		{
            this.player = new CPlayer(this, player_index);
			this.battle_room = room;
            change_state(USER_STATE_TYPE.PLAY);
		}
	}
}
