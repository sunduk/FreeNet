using FreeNet;
using GameServer.RoomState;
using GameServer.State;

namespace GameServer;

/// <summary>
/// Class containing common game-room functionality.
/// Game-specific logic is handled in each state object.
/// Packet flow: (client sends packet) -&gt; (user) -&gt; (game room) -&gt; (game room state object)
/// </summary>
public class GameRoom
{
    /// <summary>
    /// Current players.
    /// </summary>
    private readonly List<Player> _players;

    /// <summary>
    /// Tracks whether a protocol was received, and whether all players have received it.
    /// Needed for state synchronization across players.
    /// </summary>
    private readonly Dictionary<byte, PROTOCOL> _receivedProtocol;

    /// <summary>
    /// ---------------------------------------------- Common GameRoom infrastructure. ----------------------------------------------
    /// Manager that controls game rooms; used to remove the room when all players leave.
    /// </summary>
    private readonly GameRoomManager _roomManager;

    /// <summary>
    /// Index of the player currently taking a turn.
    /// </summary>
    private byte _currentTurnPlayer;

    public GameRoom(GameRoomManager room_manager)
    {
        _roomManager = room_manager;
        _players = [];
        _receivedProtocol = [];
        _currentTurnPlayer = 0;

        StateManager = new StateManager<Player, Packet>();
        StateManager.Add(STATE.READY, new GameRoomReadyState(this));
        StateManager.Add(STATE.PLAY, new GameRoomPlayState(this));
        StateManager.ChangeState(STATE.READY);
    }

    public enum STATE
    {
        READY,
        PLAY
    }

    /// <summary>
    /// Game state manager. Gameplay logic is handled by each state class.
    /// </summary>
    public StateManager<Player, Packet> StateManager { get; private set; }

    /// <summary>
    /// Errors the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void Error(Player player) => player.Disconnect();

    /// <summary>
    /// Checks whether all players have received the given protocol.
    /// Call this when client-state synchronization is required.
    /// Returns false if even one player has not received it; otherwise clears state and returns true.
    /// </summary>
    /// <param name="protocol"></param>
    /// <returns></returns>
    public bool AllReceived(PROTOCOL protocol)
    {
        if (_receivedProtocol.Count < _players.Count)
        {
            return false;
        }

        foreach (var kvp in _receivedProtocol)
        {
            if (kvp.Value != protocol)
            {
                return false;
            }
        }

        ClearReceivedProtocol();
        return true;
    }

    public void Broadcast(Packet msg)
    {
        for (var i = 0; i < _players.Count; ++i)
        {
            _players[i].Send(msg);
        }
    }

    public void ClearReceivedProtocol() => _receivedProtocol.Clear();

    public void Destroy()
    {
        var msg = Packet.Create((short)PROTOCOL.ROOM_REMOVED);
        Broadcast(msg);

        for (var i = 0; i < _players.Count; ++i)
        {
            _players[i].Removed();
        }

        _players.Clear();
    }

    /// <summary>
    /// Executes the specified function for each player.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    public void EachPlayer(Action<Player> function)
    {
        for (var i = 0; i < _players.Count; ++i)
        {
            function(_players[i]);
        }
    }

    /// <summary>
    /// Places players into the room after matching succeeds.
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="player2"></param>
    public void EnterGameRoom(Player player1, Player player2)
    {
        if (player1 is null || player2 is null)
        {
            throw new Exception("Player cannot be null.");
        }

        if (_players.Count >= 2)
        {
            throw new Exception("This room is not empty.");
        }

        AddPlayer(player1);
        AddPlayer(player2);

        var msg = Packet.Create((short)PROTOCOL.START_LOADING);
        Broadcast(msg);
    }

    /// <summary>
    /// Returns the player currently taking a turn.
    /// </summary>
    /// <returns></returns>
    public Player GetCurrentPlayer() => _players[_currentTurnPlayer];

    /// <summary>
    /// Returns the opponent player.
    /// </summary>
    /// <returns></returns>
    public Player GetOpponentPlayer(Player who) => who.PlayerIndex == 0 ? _players[1] : _players[0];

    /// <summary>
    /// Returns the opponent of the current-turn player.
    /// </summary>
    /// <returns></returns>
    public Player GetOpponentPlayer() => GetOpponentPlayer(GetCurrentPlayer());

    public Player GetPlayer(byte player_index) => _players[player_index];

    public int GetPlayerCount() => _players.Count;

    public List<Player> GetPlayers() => _players;

    /// <summary>
    /// Checks whether sender is the player currently taking a turn.
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    public bool IsCurrentPlayer(Player sender) => _currentTurnPlayer == sender.PlayerIndex;

    /// <summary>
    /// Called when a player's connection is closed.
    /// </summary>
    /// <param name="player"></param>
    public void OnPlayerRemoved(Player player)
    {
        _ = _players.Remove(player);
        if (_players.Count <= 1)
        {
            _roomManager.RemoveRoom(this);
        }
    }

    /// <summary>
    /// Called when [receive].
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="msg">The MSG.</param>
    public void OnReceive(Player owner, Packet msg)
    {
        var protocol = (PROTOCOL)msg.PopProtocolId();
        if (IsReceived(owner.PlayerIndex, protocol))
        {
            // Player already sent this protocol. Return without duplicate handling.
            return;
        }

        // Record that this protocol was received.
        CheckedProtocol(owner.PlayerIndex, protocol);

        // Forward sender and packet data to the state manager.
        // Subsequent game logic is handled by the currently active state object.
        StateManager.SendStateMessage(protocol, owner, msg);
    }

    public void RemoveSelf() => _roomManager.RemoveRoom(this);

    public void Reset() => _currentTurnPlayer = 0;

    public void TurnNext()
    {
        if (GetCurrentPlayer().PlayerIndex < GetPlayerCount() - 1)
        {
            ++_currentTurnPlayer;
        }
        else
        {
            // Wrap back to the first player's turn.
            _currentTurnPlayer = GetPlayer(0).PlayerIndex;
        }
    }

    private void AddPlayer(Player newbie) => _players.Add(newbie);

    /// <summary>
    /// Records that a player received the specified protocol.
    /// </summary>
    /// <param name="player_index"></param>
    /// <param name="protocol"></param>
    private void CheckedProtocol(byte player_index, PROTOCOL protocol)
    {
        if (_receivedProtocol.ContainsKey(player_index))
        {
            return;
        }

        _receivedProtocol.Add(player_index, protocol);
    }

    /// <summary>
    /// Checks whether a player already received the specified protocol.
    /// </summary>
    /// <param name="player_index"></param>
    /// <param name="protocol"></param>
    /// <returns></returns>
    private bool IsReceived(byte player_index, PROTOCOL protocol) => _receivedProtocol.TryGetValue(player_index, out var value) && value == protocol;
}
