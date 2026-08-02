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
    /// Game board.
    /// </summary>
    private readonly List<short> _gameBoard;

    private readonly GameRoom _room;

    /// <summary>
    /// Board index data containing indices 0 through 49.
    /// </summary>
    private readonly List<short> _tableBoard;

    public GameRoomPlayState(GameRoom room)
    {
        _room = room;
        _room.StateManager.RegisterMessageHandler(this, PROTOCOL.MOVING_REQ, MovingReq);
        _room.StateManager.RegisterMessageHandler(this, PROTOCOL.TURN_FINISHED_REQ, TurnFinished);

        // Build a 7*7 board (49 cells total). Initialize all cells to EMPTY_SLOT.
        _gameBoard = [];
        _tableBoard = [];
        for (byte i = 0; i < ColumnCount * ColumnCount; ++i)
        {
            _gameBoard.Add(_emptySlot);
            _tableBoard.Add(i);
        }
    }

    /// <summary>
    /// Infects opponent viruses.
    /// </summary>
    /// <param name="basis_cell">The basis cell for infection.</param>
    /// <param name="attacker">The player who is attacking.</param>
    /// <param name="victim">The player who is being attacked.</param>
    public void Infect(short basis_cell, Player attacker, Player victim)
    {
        // Defender viruses within distance 1 of basis position are infection targets.
        var neighbors = Helper.FindNeighborCells(basis_cell, victim.Viruses, 1);
        foreach (var position in neighbors)
        {
            // Remove defender virus.
            RemoveVirus(victim.PlayerIndex, position);

            // Add attacker virus,
            PutVirus(attacker.PlayerIndex, position);
        }
    }

    /// <summary>
    /// Handles a client move request.
    /// </summary>
    /// <param name="sender">Requesting user.</param>
    /// <param name="begin_pos">Start position.</param>
    /// <param name="target_pos">Target position to move to.</param>
    public void MovingReq(Player sender, Packet receivedData)
    {
        _room.ClearReceivedProtocol();

        var begin_pos = receivedData.PopInt16();
        var target_pos = receivedData.PopInt16();

        // Check whether it is sender's turn.
        if (!_room.IsCurrentPlayer(sender))
        {
            GameRoom.Error(sender);
            return;
        }

        // Check that sender has a virus at begin_pos.
        if (_gameBoard[begin_pos] != sender.PlayerIndex)
        {
            // No sender virus exists at the start position.
            GameRoom.Error(sender);
            return;
        }

        // Target must be an EMPTY_SLOT. Cannot move to a cell occupied by another virus.
        if (_gameBoard[target_pos] != _emptySlot)
        {
            // Another virus occupies the target position.
            GameRoom.Error(sender);
            return;
        }

        // Check whether target_pos is in move/clone range.
        var distance = Helper.GetDistance(begin_pos, target_pos);
        if (distance > 2)
        {
            // Distances over 2 cells are invalid.
            GameRoom.Error(sender);
            return;
        }

        if (distance <= 0)
        {
            // Cannot move to the same position.
            GameRoom.Error(sender);
            return;
        }

        // If all checks pass, process movement.
        if (distance == 1)      // If move distance is 1 cell, perform clone.
        {
            PutVirus(sender.PlayerIndex, target_pos);
        }
        else if (distance == 2)     // If move distance is 2 cells, perform move.
        {
            // Remove virus from previous position.
            RemoveVirus(sender.PlayerIndex, begin_pos);

            // Place virus at new position.
            PutVirus(sender.PlayerIndex, target_pos);
        }

        // Infect nearby opponent viruses around target and convert them to sender side.
        var opponent = _room.GetOpponentPlayer();
        Infect(target_pos, sender, opponent);

        // Broadcast final result.
        var msg = Packet.Create((short)PROTOCOL.PLAYER_MOVED);
        msg.Push(sender.PlayerIndex);      // Who moved
        msg.Push(begin_pos);                // From where
        msg.Push(target_pos);               // To where
        _room.Broadcast(msg);
    }

    /// <inheritdoc/>
    public void OnEnter() => BattleStart();

    /// <inheritdoc/>
    public void OnExit()
    {
    }

    /// <summary>
    /// Called when the client finishes all turn animations.
    /// </summary>
    /// <param name="sender">The player who finished the turn.</param>
    /// <param name="msg">The packet containing the turn finished request.</param>
    public void TurnFinished(Player sender, Packet msg)
    {
        if (!_room.AllReceived(PROTOCOL.TURN_FINISHED_REQ))
        {
            return;
        }

        // Advance to next turn.
        TurnEnd();
    }

    /// <summary>
    /// Starts the game.
    /// </summary>
    private void BattleStart()
    {
        // Reset data required for each new game start.
        _room.Reset();
        ResetGameData();

        _room.EachPlayer(player =>
        {
            // Send game-start message.
            var msg = Packet.Create((short)PROTOCOL.GAME_START);

            // Current player's own index.
            msg.Push(player.PlayerIndex);

            // Send all players' virus positions.
            msg.Push((byte)_room.GetPlayerCount());
            _room.EachPlayer(p =>
            {
                msg.Push(p.PlayerIndex);      // Player index used for identification.

                // Total number of viruses owned by this player.
                var cell_count = (byte)p.Viruses.Count;
                msg.Push(cell_count);
                // Position data for this player's viruses.
                p.Viruses.ForEach(position => msg.PushInt16(position));
            });

            // Player index that takes the first turn.
            msg.Push(_room.GetCurrentPlayer().PlayerIndex);

            player.Send(msg);
        });
    }

    private void GameOver()
    {
        var count_1p = _room.GetPlayer(0).GetVirusCount();
        var count_2p = _room.GetPlayer(1).GetVirusCount();

        // Determine winner.
        byte win_player_index;
        if (count_1p == count_2p)
        {
            // Tie case.
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
    /// Places a player's virus on the board.
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
    /// Places a player's virus on the board.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="position"></param>
    private void PutVirus(byte playerIndex, short position)
    {
        _gameBoard[position] = playerIndex;
        _room.GetPlayer(playerIndex).AddCell(position);
    }

    /// <summary>
    /// Removes a placed virus.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="position"></param>
    private void RemoveVirus(byte playerIndex, short position)
    {
        _gameBoard[position] = _emptySlot;
        _room.GetPlayer(playerIndex).RemoveCell(position);
    }

    /// <summary>
    /// Resets game data needed whenever a new game starts.
    /// </summary>
    private void ResetGameData()
    {
        // Reset player data.
        _room.EachPlayer(player => player.Reset());

        // Reset board data.
        for (var i = 0; i < _gameBoard.Count; ++i)
        {
            _gameBoard[i] = _emptySlot;
        }
        // Place player 1 viruses at top-left (0,0) and top-right (0,6).
        PutVirus(0, 0, 0);
        PutVirus(0, 0, 6);
        // Place player 2 viruses at bottom-left (6,0) and bottom-right (6,6).
        PutVirus(1, 6, 0);
        PutVirus(1, 6, 6);
    }

    /// <summary>
    /// Notifies clients to start the turn.
    /// </summary>
    private void StartTurn()
    {
        var msg = Packet.Create((short)PROTOCOL.START_PLAYER_TURN);
        msg.Push(_room.GetCurrentPlayer().PlayerIndex);
        _room.Broadcast(msg);
    }

    /// <summary>
    /// Ends the turn and checks whether the game has finished.
    /// </summary>
    private void TurnEnd()
    {
        // Check board state to determine whether the game is over.
        if (!Helper.CanPlayMore(_tableBoard, _room.GetOpponentPlayer(), _room.GetPlayers()))
        {
            GameOver();
            return;
        }

        // If the game is not over, pass turn to the next player.
        _room.TurnNext();

        // Start the turn.
        StartTurn();
    }
}
