using System;
using System.Collections;
using System.Collections.Generic;
using FreeNet;

namespace GameServer
{
    using RoomState;

    /// <summary>
    /// 게임방의 공통적인 기능을 담고 있는 클래스.
    /// 게임에 특화된 로직들은 각 상태에서 처리한다.
    /// 
    /// * 게임 패킷 처리 순서
    ///   - (클라이언트에서 패킷 전송) ---> (유저) ---> (게임방) ---> (게임방 상태 객체)
    /// </summary>
    public class CGameRoom
    {
        public enum STATE
        {
            READY,
            PLAY
        }

        //----------------------------------------------
        // GameRoom의 공통적인 부분.
        //----------------------------------------------
        // 게임방들을 관리하는 매니저 객체.
        // 플레이어가 모두 나갔을 때 방을 삭제하기 위해 필요하다.
        CGameRoomManager room_manager;

        // 현재 플레이어들.
        List<CPlayer> players;

        // 프로토콜을 받았는지, 모두한테서 받았는지 등을 체크하는 변수.
        // 플레이어간 상태 동기화를 위해 필요하다.
        Dictionary<byte, PROTOCOL> received_protocol;

        // 현재 턴을 진행하고 있는 플레이어의 인덱스.
        byte current_turn_player;

        // 게임 상태 관리 매니저.
        // 게임 로직 진행은 각 상태 클래스에서 처리한다.
        public CStateManager<CPlayer, CPacket> state_manager { get; private set; }


        public CGameRoom(CGameRoomManager room_manager)
        {
            this.room_manager = room_manager;
            this.players = new List<CPlayer>();
            this.received_protocol = new Dictionary<byte, PROTOCOL>();
            this.current_turn_player = 0;

            this.state_manager = new CStateManager<CPlayer, CPacket>();
            this.state_manager.add(STATE.READY, new CGameRoomReadyState(this));
            this.state_manager.add(STATE.PLAY, new CGameRoomPlayState(this));
            this.state_manager.change_state(STATE.READY);
        }


        public void reset()
        {
            this.current_turn_player = 0;
        }


        public void broadcast(CPacket msg)
        {
            for (int i = 0; i < this.players.Count; ++i)
            {
                this.players[i].send(msg);
            }
        }


        /// <summary>
        /// 매칭 성공 후 플레이어들을 방에 입장 시킨다.
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
        public void enter_gameroom(CPlayer player1, CPlayer player2)
        {
            if (player1 == null || player2 == null)
            {
                throw new Exception("Player cannot be null.");
            }

            if (this.players.Count >= 2)
            {
                throw new Exception("This room is not empty.");
            }

            add_player(player1);
            add_player(player2);

            CPacket msg = CPacket.create((short)PROTOCOL.START_LOADING);
            broadcast(msg);
        }


        void add_player(CPlayer newbie)
        {
            this.players.Add(newbie);
        }


        public void destroy()
        {
            CPacket msg = CPacket.create((short)PROTOCOL.ROOM_REMOVED);
            broadcast(msg);

            for (int i = 0; i < this.players.Count; ++i)
            {
                this.players[i].removed();
            }
            this.players.Clear();
        }


        public void remove_self()
        {
            this.room_manager.remove_room(this);
        }


        /// <summary>
        /// 플레이어가 해당 프로토콜을 이미 받았는지 체크함.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        bool is_received(byte player_index, PROTOCOL protocol)
        {
            if (!this.received_protocol.ContainsKey(player_index))
            {
                return false;
            }

            return this.received_protocol[player_index] == protocol;
        }


        /// <summary>
        /// 플레이어가 해당 프로토콜을 받았다고 기록해놓음.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="protocol"></param>
        void checked_protocol(byte player_index, PROTOCOL protocol)
        {
            if (this.received_protocol.ContainsKey(player_index))
            {
                return;
            }

            this.received_protocol.Add(player_index, protocol);
        }


        /// <summary>
        /// 모든 플레이어가 해당 프로토콜을 받았는지 체크함.
        /// 플레이어들의 클라이언트 상태 동기화가 필요할 때 호출하여 체크한다.
        /// 못받은 플레이어가 한명이라도 있다면 false를 리턴.
        /// 모두한테서 받았다면 상태를 초기화 하고 true를 리턴.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public bool all_received(PROTOCOL protocol)
        {
            if (this.received_protocol.Count < this.players.Count)
            {
                return false;
            }

            foreach (KeyValuePair<byte, PROTOCOL> kvp in this.received_protocol)
            {
                if (kvp.Value != protocol)
                {
                    return false;
                }
            }

            clear_received_protocol();
            return true;
        }


        public void clear_received_protocol()
        {
            this.received_protocol.Clear();
        }


        /// <summary>
        /// 플레이어의 접속이 끊겼을 때.
        /// </summary>
        /// <param name="player"></param>
        public void on_player_removed(CPlayer player)
        {
            this.players.Remove(player);
            if (this.players.Count <= 1)
            {
                this.room_manager.remove_room(this);
            }
        }


        /// <summary>
        /// 현재 턴을 진행중인 플레이어를 리턴한다.
        /// </summary>
        /// <returns></returns>
        public CPlayer get_current_player()
        {
            return this.players[this.current_turn_player];
        }


        public void turn_next()
        {
            if (get_current_player().player_index < get_player_count() - 1)
            {
                ++this.current_turn_player;
            }
            else
            {
                // 다시 첫번째 플레이어의 턴으로 만들어 준다.
                this.current_turn_player = get_player(0).player_index;
            }
        }


        public CPlayer get_player(byte player_index)
        {
            return this.players[player_index];
        }


        public List<CPlayer> get_players()
        {
            return this.players;
        }


        public int get_player_count()
        {
            return this.players.Count;
        }


        public void each_player(Action<CPlayer> function)
        {
            for (int i = 0; i < this.players.Count; ++i)
            {
                function(this.players[i]);
            }
        }


        /// <summary>
        /// 상대방 플레이어를 리턴한다.
        /// </summary>
        /// <returns></returns>
        public CPlayer get_opponent_player(CPlayer who)
        {
            if (who.player_index == 0)
            {
                return this.players[1];
            }

            return this.players[0];
        }


        /// <summary>
        /// 현재 턴 플레이어의 상대방 플레이어를 리턴한다.
        /// </summary>
        /// <returns></returns>
        public CPlayer get_opponent_player()
        {
            return get_opponent_player(get_current_player());
        }


        /// <summary>
        /// sender가 현재 턴을 진행중인 플레이어가 맞는지 확인한다.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool is_current_player(CPlayer sender)
        {
            return this.current_turn_player == sender.player_index;
        }


        //--------------------------------------------------------
        // Handler.
        //--------------------------------------------------------
        public void on_receive(CPlayer owner, CPacket msg)
        {
            PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();
            if (is_received(owner.player_index, protocol))
            {
                // 플레이어가 이미 해당 프로토콜을 전송했다. 중복 처리 하지 않고 리턴한다.
                return;
            }

            // 프로토콜을 받았다고 기록한다.
            checked_protocol(owner.player_index, protocol);

            // 상태 매니저에 패킷을 보낸 플레이어와 패킷 내용을 전달한다.
            // 이후 게임 로직은 상태 매니저를 통해 현재 수행중인 상태 객체에서 처리된다.
            this.state_manager.send_state_message(protocol, owner, msg);
        }


        public void error(CPlayer player)
        {
            player.disconnect();
        }
    }
}
