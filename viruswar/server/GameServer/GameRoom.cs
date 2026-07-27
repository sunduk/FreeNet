using FreeNet;
using GameServer.RoomState;
using GameServer.State;

namespace GameServer;

/// <summary>
/// 게임방의 공통적인 기능을 담고 있는 클래스. 게임에 특화된 로직들은 각 상태에서 처리한다. * 게임 패킷 처리 순서 - (클라이언트에서 패킷 전송) ---&gt; (유저) ---&gt; (게임방)
/// ---&gt; (게임방 상태 객체)
/// </summary>
public class GameRoom
{
    /// <summary>
    /// 현재 플레이어들.
    /// </summary>
    private readonly List<Player> _players;

    /// <summary>
    /// 프로토콜을 받았는지, 모두한테서 받았는지 등을 체크하는 변수. 플레이어간 상태 동기화를 위해 필요하다.
    /// </summary>
    private readonly Dictionary<byte, PROTOCOL> _receivedProtocol;

    /// <summary>
    /// ---------------------------------------------- GameRoom의 공통적인 부분. ----------------------------------------------
    /// 게임방들을 관리하는 매니저 객체. 플레이어가 모두 나갔을 때 방을 삭제하기 위해 필요하다.
    /// </summary>
    private readonly GameRoomManager _roomManager;

    /// <summary>
    /// 현재 턴을 진행하고 있는 플레이어의 인덱스.
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
    /// 게임 상태 관리 매니저. 게임 로직 진행은 각 상태 클래스에서 처리한다.
    /// </summary>
    public StateManager<Player, Packet> StateManager { get; private set; }

    /// <summary>
    /// Errors the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void Error(Player player) => player.Disconnect();

    /// <summary>
    /// 모든 플레이어가 해당 프로토콜을 받았는지 체크함. 플레이어들의 클라이언트 상태 동기화가 필요할 때 호출하여 체크한다. 못받은 플레이어가 한명이라도 있다면 false를 리턴. 모두한테서 받았다면 상태를
    /// 초기화 하고 true를 리턴.
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
    /// 매칭 성공 후 플레이어들을 방에 입장 시킨다.
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
    /// 현재 턴을 진행중인 플레이어를 리턴한다.
    /// </summary>
    /// <returns></returns>
    public Player GetCurrentPlayer() => _players[_currentTurnPlayer];

    /// <summary>
    /// 상대방 플레이어를 리턴한다.
    /// </summary>
    /// <returns></returns>
    public Player GetOpponentPlayer(Player who) => who.PlayerIndex == 0 ? _players[1] : _players[0];

    /// <summary>
    /// 현재 턴 플레이어의 상대방 플레이어를 리턴한다.
    /// </summary>
    /// <returns></returns>
    public Player GetOpponentPlayer() => GetOpponentPlayer(GetCurrentPlayer());

    public Player GetPlayer(byte player_index) => _players[player_index];

    public int GetPlayerCount() => _players.Count;

    public List<Player> GetPlayers() => _players;

    /// <summary>
    /// sender가 현재 턴을 진행중인 플레이어가 맞는지 확인한다.
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    public bool IsCurrentPlayer(Player sender) => _currentTurnPlayer == sender.PlayerIndex;

    /// <summary>
    /// 플레이어의 접속이 끊겼을 때.
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
            // 플레이어가 이미 해당 프로토콜을 전송했다. 중복 처리 하지 않고 리턴한다.
            return;
        }

        // 프로토콜을 받았다고 기록한다.
        CheckedProtocol(owner.PlayerIndex, protocol);

        // 상태 매니저에 패킷을 보낸 플레이어와 패킷 내용을 전달한다. 이후 게임 로직은 상태 매니저를 통해 현재 수행중인 상태 객체에서 처리된다.
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
            // 다시 첫번째 플레이어의 턴으로 만들어 준다.
            _currentTurnPlayer = GetPlayer(0).PlayerIndex;
        }
    }

    private void AddPlayer(Player newbie) => _players.Add(newbie);

    /// <summary>
    /// 플레이어가 해당 프로토콜을 받았다고 기록해놓음.
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
    /// 플레이어가 해당 프로토콜을 이미 받았는지 체크함.
    /// </summary>
    /// <param name="player_index"></param>
    /// <param name="protocol"></param>
    /// <returns></returns>
    private bool IsReceived(byte player_index, PROTOCOL protocol) => _receivedProtocol.TryGetValue(player_index, out var value) && value == protocol;
}
