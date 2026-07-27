using System.Net.Sockets;
using System.Reflection;
using FreeNet;
using GameServer.RoomState;

namespace GameServer.Tests;

public class GameRoomPlayStateTests
{
    [Test]
    public async Task Infect_converts_neighbor_viruses_from_victim_to_attacker()
    {
        var room = new GameRoom(new GameRoomManager());
        var attacker = CreatePlayer(0);
        var victim = CreatePlayer(1);
        room.GetPlayers().Add(attacker);
        room.GetPlayers().Add(victim);

        var state = new GameRoomPlayState(room);

        InvokePrivate(state, "PutVirus", (byte)0, (short)8);
        InvokePrivate(state, "PutVirus", (byte)1, (short)9);
        InvokePrivate(state, "PutVirus", (byte)1, (short)16);

        state.Infect(8, attacker, victim);

        _ = await Assert.That(attacker.Viruses.Contains((short)9)).IsTrue();
        _ = await Assert.That(attacker.Viruses.Contains((short)16)).IsTrue();
        _ = await Assert.That(victim.Viruses.Contains((short)9)).IsFalse();
        _ = await Assert.That(victim.Viruses.Contains((short)16)).IsFalse();
    }

    [Test]
    public async Task TurnFinished_returns_early_when_not_all_clients_reported()
    {
        var room = new GameRoom(new GameRoomManager());
        room.GetPlayers().Add(CreatePlayer(0));
        room.GetPlayers().Add(CreatePlayer(1));

        var state = new GameRoomPlayState(room);
        var packet = Packet.Create((short)PROTOCOL.TURN_FINISHED_REQ);
        packet.RecordSize();
        var received = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), null!);

        state.TurnFinished(room.GetPlayer(0), received);

        _ = await Assert.That(room.GetCurrentPlayer().PlayerIndex).IsEqualTo((byte)0);
    }

    [Test]
    public async Task MovingReq_rejects_non_current_player()
    {
        var room = new GameRoom(new GameRoomManager());
        room.GetPlayers().Add(CreatePlayer(0));
        room.GetPlayers().Add(CreatePlayer(1));

        var state = new GameRoomPlayState(room);
        InvokePrivate(state, "PutVirus", (byte)0, (short)0);

        var request = Packet.Create((short)PROTOCOL.MOVING_REQ);
        request.Push((short)0);
        request.Push((short)1);
        request.RecordSize();

        var incoming = new Packet(new ArraySegment<byte>(request.Buffer, 0, request.Position), null!);
        state.MovingReq(room.GetPlayer(1), incoming);

        _ = await Assert.That(room.GetPlayer(1).Viruses.Count).IsEqualTo(0);
    }

    private static void InvokePrivate(GameRoomPlayState state, string method, byte playerIndex, short position)
    {
        var target = typeof(GameRoomPlayState).GetMethod(
            method,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(byte), typeof(short)],
            modifiers: null);
        if (target is null)
        {
            throw new InvalidOperationException($"Missing method: {method}");
        }

        _ = target.Invoke(state, [playerIndex, position]);
    }

    private static Player CreatePlayer(byte index)
    {
        var token = new UserToken(null!);
        token.SetEventArgs(new SocketAsyncEventArgs(), new SocketAsyncEventArgs());
        token.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var user = new GameUser(token);
        return new Player(user, index);
    }
}
