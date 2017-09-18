using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.RoomState
{
    using FreeNet;

    class CGameRoomPlayState : IState
    {
        CGameRoom room;

        // 게임 보드판.
        List<short> gameboard;

        // 0~49까지의 인덱스를 갖고 있는 보드판 데이터.
        List<short> table_board;

        static byte COLUMN_COUNT = 7;

        readonly short EMPTY_SLOT = short.MaxValue;


        public CGameRoomPlayState(CGameRoom room)
        {
            this.room = room;
            this.room.state_manager.register_message_handler(this, PROTOCOL.MOVING_REQ, this.moving_req);
            this.room.state_manager.register_message_handler(this, PROTOCOL.TURN_FINISHED_REQ, this.turn_finished);

            // 7*7(총 49칸)모양의 보드판을 구성한다.
            // 초기에는 모두 빈공간이므로 EMPTY_SLOT으로 채운다.
            this.gameboard = new List<short>();
            this.table_board = new List<short>();
            for (byte i = 0; i < COLUMN_COUNT * COLUMN_COUNT; ++i)
            {
                this.gameboard.Add(EMPTY_SLOT);
                this.table_board.Add(i);
            }
        }


        void IState.on_enter()
        {
            battle_start();
        }


        void IState.on_exit()
        {
        }


        /// <summary>
        /// 게임을 시작한다.
        /// </summary>
        void battle_start()
        {
            // 게임을 새로 시작할 때 마다 초기화해줘야 할 것들.
            this.room.reset();
            reset_gamedata();

            this.room.each_player(player =>
            {
                // 게임 시작 메시지 전송.
                CPacket msg = CPacket.create((short)PROTOCOL.GAME_START);

                // 해당 플레이어 본인의 인덱스.
                msg.push(player.player_index);

                // 플레이어들의 세균 위치 전송.
                msg.push((byte)this.room.get_player_count());
                this.room.each_player(_player =>
                {
                    msg.push(_player.player_index);      // 누구인지 구분하기 위한 플레이어 인덱스.

                    // 플레이어가 소지한 세균들의 전체 개수.
                    byte cell_count = (byte)_player.viruses.Count;
                    msg.push(cell_count);
                    // 플레이어의 세균들의 위치정보.
                    _player.viruses.ForEach(position => msg.push_int16(position));
                });

                // 첫 턴을 진행할 플레이어 인덱스.
                msg.push(this.room.get_current_player().player_index);

                player.send(msg);
            });
        }


        /// <summary>
        /// 턴을 시작하라고 클라이언트들에게 알려 준다.
        /// </summary>
        void start_turn()
        {
            CPacket msg = CPacket.create((short)PROTOCOL.START_PLAYER_TURN);
            msg.push(this.room.get_current_player().player_index);
            this.room.broadcast(msg);
        }


        /// <summary>
        /// 게임 데이터를 초기화 한다.
        /// 게임을 새로 시작할 때 마다 초기화 해줘야 할 것들을 넣는다.
        /// </summary>
        void reset_gamedata()
        {
            // 플레이어 데이터 초기화.
            this.room.each_player(player => player.reset());

            // 보드판 데이터 초기화.
            for (int i = 0; i < this.gameboard.Count; ++i)
            {
                this.gameboard[i] = EMPTY_SLOT;
            }
            // 1번 플레이어의 세균은 왼쪽위(0,0), 오른쪽위(0,6) 두군데에 배치한다.
            put_virus(0, 0, 0);
            put_virus(0, 0, 6);
            // 2번 플레이어는 세균은 왼쪽아래(6,0), 오른쪽아래(6,6) 두군데에 배치한다.
            put_virus(1, 6, 0);
            put_virus(1, 6, 6);
        }


        /// <summary>
        /// 보드판에 플레이어의 세균을 배치한다.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        void put_virus(byte player_index, byte row, byte col)
        {
            short position = CHelper.get_position(row, col);
            put_virus(player_index, position);
        }


        /// <summary>
        /// 보드판에 플레이어의 세균을 배치한다.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="position"></param>
        void put_virus(byte player_index, short position)
        {
            this.gameboard[position] = player_index;
            this.room.get_player(player_index).add_cell(position);
        }


        /// <summary>
        /// 배치된 세균을 삭제한다.
        /// </summary>
        /// <param name="player_index"></param>
        /// <param name="position"></param>
        void remove_virus(byte player_index, short position)
        {
            this.gameboard[position] = EMPTY_SLOT;
            this.room.get_player(player_index).remove_cell(position);
        }


        /// <summary>
        /// 상대방의 세균을 감염 시킨다.
        /// </summary>
        /// <param name="basis_cell"></param>
        /// <param name="attacker"></param>
        /// <param name="victim"></param>
        public void infect(short basis_cell, CPlayer attacker, CPlayer victim)
        {
            // 방어자의 세균중에 기준위치로 부터 1칸 반경에 있는 세균들이 감염 대상이다.
            List<short> neighbors = CHelper.find_neighbor_cells(basis_cell, victim.viruses, 1);
            foreach (short position in neighbors)
            {
                // 방어자의 세균을 삭제한다.
                remove_virus(victim.player_index, position);

                // 공격자의 세균을 추가하고,
                put_virus(attacker.player_index, position);
            }
        }


        /// <summary>
        /// 클라이언트의 이동 요청.
        /// </summary>
        /// <param name="sender">요청한 유저</param>
        /// <param name="begin_pos">시작 위치</param>
        /// <param name="target_pos">이동하고자 하는 위치</param>
        public void moving_req(CPlayer sender, CPacket received_data)
        {
            this.room.clear_received_protocol();

            short begin_pos = received_data.pop_int16();
            short target_pos = received_data.pop_int16();

            // sender차례인지 체크.
            if (!this.room.is_current_player(sender))
            {
                this.room.error(sender);
                return;
            }

            // begin_pos에 sender의 세균이 존재하는지 체크.
            if (this.gameboard[begin_pos] != sender.player_index)
            {
                // 시작 위치에 해당 플레이어의 세균이 존재하지 않는다.
                this.room.error(sender);
                return;
            }

            // 목적지는 EMPTY_SLOT으로 설정된 빈 공간이어야 한다.
            // 다른 세균이 자리하고 있는 곳으로는 이동할 수 없다.
            if (this.gameboard[target_pos] != EMPTY_SLOT)
            {
                // 목적지에 다른 세균이 존재한다.
                this.room.error(sender);
                return;
            }

            // target_pos가 이동 또는 복제 가능한 범위인지 체크.
            short distance = CHelper.get_distance(begin_pos, target_pos);
            if (distance > 2)
            {
                // 2칸을 초과하는 거리는 이동할 수 없다.
                this.room.error(sender);
                return;
            }

            if (distance <= 0)
            {
                // 자기 자신의 위치로는 이동할 수 없다.
                this.room.error(sender);
                return;
            }

            // 모든 체크가 정상이라면 이동을 처리한다.
            if (distance == 1)      // 이동 거리가 한칸일 경우에는 복제를 수행한다.
            {
                put_virus(sender.player_index, target_pos);
            }
            else if (distance == 2)     // 이동 거리가 두칸일 경우에는 이동을 수행한다.
            {
                // 이전 위치에 있는 세균은 삭제한다.
                remove_virus(sender.player_index, begin_pos);

                // 새로운 위치에 세균을 놓는다.
                put_virus(sender.player_index, target_pos);
            }

            // 목적지를 기준으로 주위에 존재하는 상대방 세균을 감염시켜 같은 편으로 만든다.
            CPlayer opponent = this.room.get_opponent_player();
            infect(target_pos, sender, opponent);

            // 최종 결과를 broadcast한다.
            CPacket msg = CPacket.create((short)PROTOCOL.PLAYER_MOVED);
            msg.push(sender.player_index);      // 누가
            msg.push(begin_pos);                // 어디서
            msg.push(target_pos);               // 어디로 이동 했는지
            this.room.broadcast(msg);
        }


        /// <summary>
        /// 클라이언트에서 턴 연출이 모두 완료 되었을 때 호출된다.
        /// </summary>
        /// <param name="sender"></param>
        public void turn_finished(CPlayer sender, CPacket msg)
        {
            if (!this.room.all_received(PROTOCOL.TURN_FINISHED_REQ))
            {
                return;
            }

            // 턴을 넘긴다.
            turn_end();
        }


        /// <summary>
        /// 턴을 종료한다. 게임이 끝났는지 확인하는 과정을 수행한다.
        /// </summary>
        void turn_end()
        {
            // 보드판 상태를 확인하여 게임이 끝났는지 검사한다.
            if (!CHelper.can_play_more(this.table_board, this.room.get_opponent_player(), this.room.get_players()))
            {
                game_over();
                return;
            }

            // 아직 게임이 끝나지 않았다면 다음 플레이어로 턴을 넘긴다.
            this.room.turn_next();

            // 턴을 시작한다.
            start_turn();
        }


        void game_over()
        {
            // 우승자 가리기.
            byte win_player_index = byte.MaxValue;
            int count_1p = this.room.get_player(0).get_virus_count();
            int count_2p = this.room.get_player(1).get_virus_count();

            if (count_1p == count_2p)
            {
                // 동점인 경우.
                win_player_index = byte.MaxValue;
            }
            else
            {
                if (count_1p > count_2p)
                {
                    win_player_index = this.room.get_player(0).player_index;
                }
                else
                {
                    win_player_index = this.room.get_player(1).player_index;
                }
            }


            CPacket msg = CPacket.create((short)PROTOCOL.GAME_OVER);
            msg.push(win_player_index);
            msg.push(count_1p);
            msg.push(count_2p);
            this.room.broadcast(msg);

            this.room.remove_self();
        }
    }
}
