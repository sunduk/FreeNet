using System.Net.Sockets;
using System.Reflection;
using FreeNet;

namespace GameServer.Tests;

public class GameRoomTests
{
    // ------------------------------------------------------------------ GameRoom --

    [Test]
    public async Task EnterGameRoom_throws_for_null_player1()
    {
        var room = new GameRoom(new GameRoomManager());
        var threw = false;
        try { room.EnterGameRoom(null!, CreatePlayer(1)); }
        catch (Exception) { threw = true; }

        _ = await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task EnterGameRoom_throws_for_null_player2()
    {
        var room = new GameRoom(new GameRoomManager());
        var threw = false;
        try { room.EnterGameRoom(CreatePlayer(0), null!); }
        catch (Exception) { threw = true; }

        _ = await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task EnterGameRoom_throws_when_room_already_has_two_players()
    {
        var room = new GameRoom(new GameRoomManager());
        room.GetPlayers().Add(CreatePlayer(0));
        room.GetPlayers().Add(CreatePlayer(1));

        var threw = false;
        try { room.EnterGameRoom(CreatePlayer(0), CreatePlayer(1)); }
        catch (Exception) { threw = true; }

        _ = await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task EnterGameRoom_adds_players_before_broadcasting()
    {
        var room = new GameRoom(new GameRoomManager());
        // Players with real sockets so StartSend fails gracefully instead of NRE
        try { room.EnterGameRoom(CreateBoundPlayer(0), CreateBoundPlayer(1)); }
        catch { /* broadcast fails on unconnected sockets – expected */ }

        _ = await Assert.That(room.GetPlayerCount()).IsEqualTo(2);
    }

    [Test]
    public async Task GetOpponentPlayer_returns_the_other_player()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        var p2 = CreatePlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        _ = await Assert.That(ReferenceEquals(room.GetOpponentPlayer(p1), p2)).IsTrue();
        _ = await Assert.That(ReferenceEquals(room.GetOpponentPlayer(p2), p1)).IsTrue();
    }

    [Test]
    public async Task GetOpponentPlayer_overload_uses_current_player()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        var p2 = CreatePlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        _ = await Assert.That(ReferenceEquals(room.GetOpponentPlayer(), p2)).IsTrue();
    }

    [Test]
    public async Task IsCurrentPlayer_returns_true_only_for_turn_player()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        var p2 = CreatePlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        _ = await Assert.That(room.IsCurrentPlayer(p1)).IsTrue();
        _ = await Assert.That(room.IsCurrentPlayer(p2)).IsFalse();
    }

    [Test]
    public async Task EachPlayer_invokes_action_for_all_players()
    {
        var room = new GameRoom(new GameRoomManager());
        room.GetPlayers().Add(CreatePlayer(0));
        room.GetPlayers().Add(CreatePlayer(1));

        var count = 0;
        room.EachPlayer(_ => count++);

        _ = await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task GetPlayer_returns_correct_player_at_index()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        var p2 = CreatePlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        _ = await Assert.That(ReferenceEquals(room.GetPlayer(0), p1)).IsTrue();
        _ = await Assert.That(ReferenceEquals(room.GetPlayer(1), p2)).IsTrue();
        _ = await Assert.That(room.GetPlayerCount()).IsEqualTo(2);
    }

    [Test]
    public async Task AllReceived_returns_true_when_all_players_sent_same_protocol()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreateBoundPlayer(0);
        var p2 = CreateBoundPlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        var bytes = BuildPacket((short)PROTOCOL.MOVING_REQ);
        room.OnReceive(p1, new Packet(new ArraySegment<byte>(bytes, 0, bytes.Length), null!));
        room.OnReceive(p2, new Packet(new ArraySegment<byte>(bytes, 0, bytes.Length), null!));

        _ = await Assert.That(room.AllReceived(PROTOCOL.MOVING_REQ)).IsTrue();
        _ = await Assert.That(room.AllReceived(PROTOCOL.MOVING_REQ)).IsFalse(); // cleared
    }

    [Test]
    public async Task AllReceived_returns_false_when_protocols_differ()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        var p2 = CreatePlayer(1);
        room.GetPlayers().Add(p1);
        room.GetPlayers().Add(p2);

        var readyBytes = BuildPacket((short)PROTOCOL.READY_TO_START);
        var otherBytes = BuildPacket((short)PROTOCOL.TURN_FINISHED_REQ);

        room.OnReceive(p1, new Packet(new ArraySegment<byte>(readyBytes, 0, readyBytes.Length), null!));
        room.OnReceive(p2, new Packet(new ArraySegment<byte>(otherBytes, 0, otherBytes.Length), null!));

        _ = await Assert.That(room.AllReceived(PROTOCOL.READY_TO_START)).IsFalse();
    }

    [Test]
    public async Task OnReceive_ignores_duplicate_protocol_from_same_player()
    {
        var room = new GameRoom(new GameRoomManager());
        var p1 = CreatePlayer(0);
        room.GetPlayers().Add(p1);

        var bytes = BuildPacket((short)PROTOCOL.MOVING_REQ);
        room.OnReceive(p1, new Packet(new ArraySegment<byte>(bytes, 0, bytes.Length), null!));
        room.OnReceive(p1, new Packet(new ArraySegment<byte>(bytes, 0, bytes.Length), null!));

        _ = await Assert.That(room.AllReceived(PROTOCOL.MOVING_REQ)).IsTrue();
    }

    // --------------------------------------------------------------- Player --

    [Test]
    public async Task Player_add_remove_cell_get_virus_count_and_reset_work()
    {
        var token = new UserToken(null!);
        var user = new GameUser(token);
        var player = new Player(user, 2);

        player.AddCell(10);
        player.AddCell(20);
        _ = await Assert.That(player.GetVirusCount()).IsEqualTo(2);

        player.RemoveCell(10);
        _ = await Assert.That(player.GetVirusCount()).IsEqualTo(1);
        _ = await Assert.That(player.Viruses.Contains((short)20)).IsTrue();

        player.Reset();
        _ = await Assert.That(player.GetVirusCount()).IsEqualTo(0);
    }

    // ------------------------------------------------------- GameServerImpl --

    [Test]
    public async Task GameServerImpl_matchingreq_adds_first_user_to_waiting_list()
    {
        var impl = new GameServerImpl();
        var user = CreateUserWithBoundToken();

        try { impl.MatchingReq(user); } catch { /* send failure on unconnected socket */ }

        _ = await Assert.That(GetWaitingList(impl).Contains(user)).IsTrue();
    }

    [Test]
    public async Task GameServerImpl_matchingreq_does_not_add_duplicate_user()
    {
        var impl = new GameServerImpl();
        var user = CreateUserWithBoundToken();
        GetWaitingList(impl).Add(user); // pre-add

        try { impl.MatchingReq(user); } catch { }

        _ = await Assert.That(GetWaitingList(impl).Count).IsEqualTo(1);
    }

    // ---------------------------------------------------------------- GameUser --

    [Test]
    public async Task GameUser_change_state_and_enter_room_work()
    {
        var token = new UserToken(null!);
        var user = new GameUser(token);

        user.EnterRoom(new GameRoom(new GameRoomManager()), 0);

        _ = await Assert.That(user.Player is not null).IsTrue();
        _ = await Assert.That(user.Player?.PlayerIndex).IsEqualTo((byte)0);
    }

    // ---------------------------------------------------------------- helpers --

    private static Player CreatePlayer(byte index)
    {
        var token = new UserToken(null!);
        return new Player(new GameUser(token), index);
    }

    private static Player CreateBoundPlayer(byte index) =>
        new(CreateUserWithBoundToken(), index);

    private static GameUser CreateUserWithBoundToken()
    {
        var token = new UserToken(null!);
        var recvArgs = new SocketAsyncEventArgs();
        recvArgs.SetBuffer(new byte[1024], 0, 1024);
        token.SetEventArgs(recvArgs, new SocketAsyncEventArgs());
        token.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        return new GameUser(token);
    }

    private static byte[] BuildPacket(short protocol)
    {
        var p = Packet.Create(protocol);
        p.RecordSize();
        var bytes = new byte[p.Position];
        Array.Copy(p.Buffer, bytes, p.Position);
        return bytes;
    }

    private static List<GameUser> GetWaitingList(GameServerImpl impl)
    {
        var field = typeof(GameServerImpl).GetField(
            "_matchingWaitingUsers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (List<GameUser>)field!.GetValue(impl)!;
    }
}
