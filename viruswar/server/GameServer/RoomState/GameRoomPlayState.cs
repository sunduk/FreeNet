using FreeNet;
using GameServer.State;

namespace GameServer.RoomState;

internal class GameRoomPlayState : IState
{
    /// <summary>
    /// The number of columns in the game board.
    /// </summary>
    private static readonly byte ColumnCount = 7;

    private readonly short _emptySlot = short.MaxValue;

    /// <summary>
    /// 게임 보드판.
    /// </summary>
    private readonly List<short> _gameBoard;

    private readonly GameRoom _room;

    /// <summary>
    /// 0~49까지의 인덱스를 갖고 있는 보드판 데이터.
    /// </summary>
    private readonly List<short> _tableBoard;

    public GameRoomPlayState(GameRoom room)
    {
        _room = room;
        _room.StateManager.RegisterMessageHandler(this, PROTOCOL.MOVING_REQ, MovingReq);
        _room.StateManager.RegisterMessageHandler(this, PROTOCOL.TURN_FINISHED_REQ, TurnFinished);

        // 7*7(총 49칸)모양의 보드판을 구성한다. 초기에는 모두 빈공간이므로 EMPTY_SLOT으로 채운다.
        _gameBoard = [];
        _tableBoard = [];
        for (byte i = 0; i < ColumnCount * ColumnCount; ++i)
        {
            _gameBoard.Add(_emptySlot);
            _tableBoard.Add(i);
        }
    }

    /// <summary>
    /// 상대방의 세균을 감염 시킨다.
    /// </summary>
    /// <param name="basis_cell">The basis cell for infection.</param>
    /// <param name="attacker">The player who is attacking.</param>
    /// <param name="victim">The player who is being attacked.</param>
    public void Infect(short basis_cell, Player attacker, Player victim)
    {
        // 방어자의 세균중에 기준위치로 부터 1칸 반경에 있는 세균들이 감염 대상이다.
        var neighbors = Helper.FindNeighborCells(basis_cell, victim.Viruses, 1);
        foreach (var position in neighbors)
        {
            // 방어자의 세균을 삭제한다.
            RemoveVirus(victim.PlayerIndex, position);

            // 공격자의 세균을 추가하고,
            PutVirus(attacker.PlayerIndex, position);
        }
    }

    /// <summary>
    /// 클라이언트의 이동 요청.
    /// </summary>
    /// <param name="sender">요청한 유저</param>
    /// <param name="begin_pos">시작 위치</param>
    /// <param name="target_pos">이동하고자 하는 위치</param>
    public void MovingReq(Player sender, Packet receivedData)
    {
        _room.ClearReceivedProtocol();

        var begin_pos = receivedData.PopInt16();
        var target_pos = receivedData.PopInt16();

        // sender차례인지 체크.
        if (!_room.IsCurrentPlayer(sender))
        {
            GameRoom.Error(sender);
            return;
        }

        // begin_pos에 sender의 세균이 존재하는지 체크.
        if (_gameBoard[begin_pos] != sender.PlayerIndex)
        {
            // 시작 위치에 해당 플레이어의 세균이 존재하지 않는다.
            GameRoom.Error(sender);
            return;
        }

        // 목적지는 EMPTY_SLOT으로 설정된 빈 공간이어야 한다. 다른 세균이 자리하고 있는 곳으로는 이동할 수 없다.
        if (_gameBoard[target_pos] != _emptySlot)
        {
            // 목적지에 다른 세균이 존재한다.
            GameRoom.Error(sender);
            return;
        }

        // target_pos가 이동 또는 복제 가능한 범위인지 체크.
        var distance = Helper.GetDistance(begin_pos, target_pos);
        if (distance > 2)
        {
            // 2칸을 초과하는 거리는 이동할 수 없다.
            GameRoom.Error(sender);
            return;
        }

        if (distance <= 0)
        {
            // 자기 자신의 위치로는 이동할 수 없다.
            GameRoom.Error(sender);
            return;
        }

        // 모든 체크가 정상이라면 이동을 처리한다.
        if (distance == 1)      // 이동 거리가 한칸일 경우에는 복제를 수행한다.
        {
            PutVirus(sender.PlayerIndex, target_pos);
        }
        else if (distance == 2)     // 이동 거리가 두칸일 경우에는 이동을 수행한다.
        {
            // 이전 위치에 있는 세균은 삭제한다.
            RemoveVirus(sender.PlayerIndex, begin_pos);

            // 새로운 위치에 세균을 놓는다.
            PutVirus(sender.PlayerIndex, target_pos);
        }

        // 목적지를 기준으로 주위에 존재하는 상대방 세균을 감염시켜 같은 편으로 만든다.
        var opponent = _room.GetOpponentPlayer();
        Infect(target_pos, sender, opponent);

        // 최종 결과를 broadcast한다.
        var msg = Packet.Create((short)PROTOCOL.PLAYER_MOVED);
        msg.Push(sender.PlayerIndex);      // 누가
        msg.Push(begin_pos);                // 어디서
        msg.Push(target_pos);               // 어디로 이동 했는지
        _room.Broadcast(msg);
    }

    /// <inheritdoc/>
    public void OnEnter() => BattleStart();

    /// <inheritdoc/>
    public void OnExit()
    {
    }

    /// <summary>
    /// 클라이언트에서 턴 연출이 모두 완료 되었을 때 호출된다.
    /// </summary>
    /// <param name="sender">The player who finished the turn.</param>
    /// <param name="msg">The packet containing the turn finished request.</param>
    public void TurnFinished(Player sender, Packet msg)
    {
        if (!_room.AllReceived(PROTOCOL.TURN_FINISHED_REQ))
        {
            return;
        }

        // 턴을 넘긴다.
        TurnEnd();
    }

    /// <summary>
    /// 게임을 시작한다.
    /// </summary>
    private void BattleStart()
    {
        // 게임을 새로 시작할 때 마다 초기화해줘야 할 것들.
        _room.Reset();
        ResetGameData();

        _room.EachPlayer(player =>
        {
            // 게임 시작 메시지 전송.
            var msg = Packet.Create((short)PROTOCOL.GAME_START);

            // 해당 플레이어 본인의 인덱스.
            msg.Push(player.PlayerIndex);

            // 플레이어들의 세균 위치 전송.
            msg.Push((byte)_room.GetPlayerCount());
            _room.EachPlayer(p =>
            {
                msg.Push(p.PlayerIndex);      // 누구인지 구분하기 위한 플레이어 인덱스.

                // 플레이어가 소지한 세균들의 전체 개수.
                var cell_count = (byte)p.Viruses.Count;
                msg.Push(cell_count);
                // 플레이어의 세균들의 위치정보.
                p.Viruses.ForEach(position => msg.PushInt16(position));
            });

            // 첫 턴을 진행할 플레이어 인덱스.
            msg.Push(_room.GetCurrentPlayer().PlayerIndex);

            player.Send(msg);
        });
    }

    private void GameOver()
    {
        var count_1p = _room.GetPlayer(0).GetVirusCount();
        var count_2p = _room.GetPlayer(1).GetVirusCount();

        // 우승자 가리기.
        byte win_player_index;
        if (count_1p == count_2p)
        {
            // 동점인 경우.
            win_player_index = byte.MaxValue;
        }
        else
        {
            win_player_index = count_1p > count_2p ? _room.GetPlayer(0).PlayerIndex : _room.GetPlayer(1).PlayerIndex;
        }

        var msg = Packet.Create((short)PROTOCOL.GAME_OVER);
        msg.Push(win_player_index);
        msg.Push(count_1p);
        msg.Push(count_2p);
        _room.Broadcast(msg);

        _room.RemoveSelf();
    }

    /// <summary>
    /// 보드판에 플레이어의 세균을 배치한다.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private void PutVirus(byte playerIndex, byte row, byte col)
    {
        var position = Helper.GetPosition(row, col);
        PutVirus(playerIndex, position);
    }

    /// <summary>
    /// 보드판에 플레이어의 세균을 배치한다.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="position"></param>
    private void PutVirus(byte playerIndex, short position)
    {
        _gameBoard[position] = playerIndex;
        _room.GetPlayer(playerIndex).AddCell(position);
    }

    /// <summary>
    /// 배치된 세균을 삭제한다.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="position"></param>
    private void RemoveVirus(byte playerIndex, short position)
    {
        _gameBoard[position] = _emptySlot;
        _room.GetPlayer(playerIndex).RemoveCell(position);
    }

    /// <summary>
    /// 게임 데이터를 초기화 한다. 게임을 새로 시작할 때 마다 초기화 해줘야 할 것들을 넣는다.
    /// </summary>
    private void ResetGameData()
    {
        // 플레이어 데이터 초기화.
        _room.EachPlayer(player => player.Reset());

        // 보드판 데이터 초기화.
        for (var i = 0; i < _gameBoard.Count; ++i)
        {
            _gameBoard[i] = _emptySlot;
        }
        // 1번 플레이어의 세균은 왼쪽위(0,0), 오른쪽위(0,6) 두군데에 배치한다.
        PutVirus(0, 0, 0);
        PutVirus(0, 0, 6);
        // 2번 플레이어는 세균은 왼쪽아래(6,0), 오른쪽아래(6,6) 두군데에 배치한다.
        PutVirus(1, 6, 0);
        PutVirus(1, 6, 6);
    }

    /// <summary>
    /// 턴을 시작하라고 클라이언트들에게 알려 준다.
    /// </summary>
    private void StartTurn()
    {
        var msg = Packet.Create((short)PROTOCOL.START_PLAYER_TURN);
        msg.Push(_room.GetCurrentPlayer().PlayerIndex);
        _room.Broadcast(msg);
    }

    /// <summary>
    /// 턴을 종료한다. 게임이 끝났는지 확인하는 과정을 수행한다.
    /// </summary>
    private void TurnEnd()
    {
        // 보드판 상태를 확인하여 게임이 끝났는지 검사한다.
        if (!Helper.CanPlayMore(_tableBoard, _room.GetOpponentPlayer(), _room.GetPlayers()))
        {
            GameOver();
            return;
        }

        // 아직 게임이 끝나지 않았다면 다음 플레이어로 턴을 넘긴다.
        _room.TurnNext();

        // 턴을 시작한다.
        StartTurn();
    }
}
