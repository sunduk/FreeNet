using FreeNet;
using GameServer.State;

namespace GameServer.Tests;

public class GameServerUtilityTests
{
    private enum TestStateKey
    {
        Lobby,
        Play
    }

    private enum TestMessageKey
    {
        Ping,
        Pong
    }

    [Test]
    public async Task MessageDispatcher_register_dispatch_overwrite_and_unregister_work()
    {
        var dispatcher = new MessageDispatcher<int, string>();
        var called = 0;
        var lastPayload = string.Empty;

        dispatcher.Register(TestMessageKey.Ping, (number, text) =>
        {
            called += number;
            lastPayload = text;
        });

        dispatcher.Dispatch(TestMessageKey.Ping, 2, "first");
        dispatcher.Register(TestMessageKey.Ping, (number, text) =>
        {
            called += number * 10;
            lastPayload = text;
        });

        dispatcher.Dispatch(TestMessageKey.Ping, 1, "second");
        dispatcher.Unregister(TestMessageKey.Ping);
        dispatcher.Dispatch(TestMessageKey.Ping, 9, "ignored");

        _ = await Assert.That(called).IsEqualTo(12);
        _ = await Assert.That(lastPayload).IsEqualTo("second");
    }

    [Test]
    public async Task StateManager_transitions_and_dispatches_only_for_current_state()
    {
        var manager = new StateManager<int, string>();
        var lobby = new FakeState();
        var play = new FakeState();
        var handled = 0;

        manager.Add(TestStateKey.Lobby, lobby);
        manager.Add(TestStateKey.Play, play);

        manager.SendStateMessage(TestMessageKey.Ping, 1, "no state yet");

        manager.RegisterMessageHandler(lobby, TestMessageKey.Ping, (number, _) => handled += number);
        manager.ChangeState(TestStateKey.Lobby);
        manager.SendStateMessage(TestMessageKey.Ping, 3, "lobby");

        manager.ChangeState(TestStateKey.Play);
        manager.SendStateMessage(TestMessageKey.Ping, 5, "play has no handler");

        _ = await Assert.That(lobby.EnterCount).IsEqualTo(1);
        _ = await Assert.That(lobby.ExitCount).IsEqualTo(1);
        _ = await Assert.That(play.EnterCount).IsEqualTo(1);
        _ = await Assert.That(handled).IsEqualTo(3);
        _ = await Assert.That(manager.IsCurrentState(TestStateKey.Play)).IsTrue();
    }

    [Test]
    public async Task StateManager_unregister_stops_future_dispatches()
    {
        var manager = new StateManager<int, int>();
        var state = new FakeState();
        var count = 0;

        manager.Add(TestStateKey.Lobby, state);
        manager.RegisterMessageHandler(state, TestMessageKey.Pong, (left, right) => count += left + right);
        manager.ChangeState(TestStateKey.Lobby);

        manager.SendStateMessage(TestMessageKey.Pong, 2, 3);
        manager.UnregisterMessageHandler(state, TestMessageKey.Pong);
        manager.SendStateMessage(TestMessageKey.Pong, 7, 8);

        _ = await Assert.That(count).IsEqualTo(5);
    }

    [Test]
    public async Task Helper_calculates_coordinates_and_distances()
    {
        _ = await Assert.That(Helper.CalcRow(17)).IsEqualTo((short)2);
        _ = await Assert.That(Helper.CalcColumn(17)).IsEqualTo((short)3);
        _ = await Assert.That(Helper.GetPosition(6, 6)).IsEqualTo((short)48);
        _ = await Assert.That(Helper.GetDistance(0, 8)).IsEqualTo((short)1);
        _ = await Assert.That(Helper.HowFarFromClickedCell(0, 16)).IsEqualTo((byte)2);
    }

    [Test]
    public async Task Helper_neighbor_and_available_cell_queries_filter_occupied_cells()
    {
        var allCells = new List<short> { 0, 1, 2, 7, 8, 14 };
        var playerA = CreatePlayer(0, 0, 1);
        var playerB = CreatePlayer(1, 8);
        var players = new List<Player> { playerA, playerB };

        var neighbors = Helper.FindNeighborCells(0, allCells, 2);
        var available = Helper.FindAvailableCells(0, allCells, players);

        _ = await Assert.That(neighbors.Contains((short)14)).IsTrue();
        _ = await Assert.That(available.Contains((short)1)).IsFalse();
        _ = await Assert.That(available.Contains((short)8)).IsFalse();
        _ = await Assert.That(available.Contains((short)2)).IsTrue();
    }

    [Test]
    public async Task Helper_canplaymore_reflects_available_moves()
    {
        var board = Enumerable.Range(0, 49).Select(static n => (short)n).ToList();

        var movable = CreatePlayer(0, 0);
        var blockedCells = Enumerable.Range(1, 48).Select(static n => (short)n).ToArray();
        var blockedOpponent = CreatePlayer(1, blockedCells);

        var canContinue = Helper.CanPlayMore(board, movable, new List<Player> { movable, CreatePlayer(1, 48) });
        var cannotContinue = Helper.CanPlayMore(board, movable, new List<Player> { movable, blockedOpponent });

        _ = await Assert.That(canContinue).IsTrue();
        _ = await Assert.That(cannotContinue).IsFalse();
    }

    [Test]
    public async Task Helper_shuffle_preserves_all_elements()
    {
        var values = Enumerable.Range(1, 20).ToList();
        Helper.Shuffle(values);
        values.Sort();

        _ = await Assert.That(values.SequenceEqual(Enumerable.Range(1, 20))).IsTrue();
    }

    [Test]
    public async Task Vector2_subtraction_returns_axis_differences()
    {
        var result = new Vector2(6, 1) - new Vector2(2, 5);

        _ = await Assert.That(result.x).IsEqualTo(4);
        _ = await Assert.That(result.y).IsEqualTo(-4);
    }

    [Test]
    public async Task PlayerMovingData_initializes_positions_and_accelerations()
    {
        var data = new PlayerMovingData(2, 1.5f, 2.5f, 3.5f);

        _ = await Assert.That(data.PlayerIndex).IsEqualTo((byte)2);
        _ = await Assert.That(data.PositionX).IsEqualTo(1.5f);
        _ = await Assert.That(data.PositionY).IsEqualTo(2.5f);
        _ = await Assert.That(data.PositionZ).IsEqualTo(3.5f);
        _ = await Assert.That(data.Accelerations.Count).IsEqualTo(Enum.GetValues<MOVE_DIRECTION>().Length);
        _ = await Assert.That(data.Accelerations.Values.All(static value => value == 0.0f)).IsTrue();
    }

    [Test]
    public async Task GameRoom_onreceive_tracks_protocol_and_allreceived_resets_state()
    {
        var room = new GameRoom(new GameRoomManager());
        var players = room.GetPlayers();
        var owner = CreatePlayer(0, 0);
        players.Add(owner);

        var movingPacketBytes = BuildProtocolOnlyPacket((short)PROTOCOL.MOVING_REQ);
        var packet = new Packet(new ArraySegment<byte>(movingPacketBytes, 0, movingPacketBytes.Length), null!);

        room.OnReceive(owner, packet);

        _ = await Assert.That(room.AllReceived(PROTOCOL.MOVING_REQ)).IsTrue();
        _ = await Assert.That(room.AllReceived(PROTOCOL.MOVING_REQ)).IsFalse();
    }

    [Test]
    public async Task GameRoom_turnnext_cycles_between_players()
    {
        var room = new GameRoom(new GameRoomManager());
        var players = room.GetPlayers();
        players.Add(CreatePlayer(0, 0));
        players.Add(CreatePlayer(1, 48));

        _ = await Assert.That(room.GetCurrentPlayer().PlayerIndex).IsEqualTo((byte)0);

        room.TurnNext();
        _ = await Assert.That(room.GetCurrentPlayer().PlayerIndex).IsEqualTo((byte)1);

        room.TurnNext();
        _ = await Assert.That(room.GetCurrentPlayer().PlayerIndex).IsEqualTo((byte)0);
    }

    private static byte[] BuildProtocolOnlyPacket(short protocol)
    {
        var packet = Packet.Create(protocol);
        packet.RecordSize();

        var copy = new byte[packet.Position];
        Array.Copy(packet.Buffer, copy, packet.Position);
        return copy;
    }

    private static Player CreatePlayer(byte index, params short[] viruses)
    {
        var token = new UserToken(null!);
        var user = new GameUser(token);
        var player = new Player(user, index);

        foreach (var virus in viruses)
        {
            player.AddCell(virus);
        }

        return player;
    }

    private sealed class FakeState : IState
    {
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        public void OnEnter() => EnterCount++;

        public void OnExit() => ExitCount++;
    }
}
