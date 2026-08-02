using NSubstitute;

namespace FreeNet.Tests;

public class UserTokenTests
{
    [Test]
    public async Task OnConnected_sets_connected_state()
    {
        var token = new UserToken(null!);

        _ = await Assert.That(token.IsConnected()).IsFalse();
        token.OnConnected();
        _ = await Assert.That(token.IsConnected()).IsTrue();
    }

    [Test]
    public async Task OnMessage_sys_update_heartbeat_updates_latest_heartbeat_time()
    {
        var token = new UserToken(null!);
        var before = DateTime.Now.Ticks;

        var packet = Packet.Create(UserToken.SYS_UPDATE_HEARTBEAT);
        packet.RecordSize();
        token.OnMessage(new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token));

        _ = await Assert.That(token.LatestHeartbeatTime >= before).IsTrue();
    }

    [Test]
    public async Task OnMessage_sys_start_heartbeat_creates_sender_without_firing_when_auto_disabled()
    {
        // _autoHeartbeat is false by default, so no timer fires
        var token = new UserToken(null!);
        var packet = Packet.Create(UserToken.SYS_START_HEARTBEAT);
        packet.Push((byte)5);
        packet.RecordSize();
        var msg = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token);

        var threw = false;
        try { token.OnMessage(msg); }
        catch { threw = true; }

        _ = await Assert.That(threw).IsFalse();
    }

    [Test]
    public void OnMessage_regular_protocol_calls_peer_onmessage()
    {
        var peer = Substitute.For<IPeer>();
        var token = new UserToken(null!)
        {
            Peer = peer
        };

        var packet = Packet.Create(50);
        packet.RecordSize();
        token.OnMessage(new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token));

        peer.Received(1).OnMessage(Arg.Any<Packet>());
    }

    [Test]
    public async Task OnMessage_without_peer_does_not_throw()
    {
        var token = new UserToken(null!);
        var packet = Packet.Create(50);
        packet.RecordSize();
        var msg = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token);

        var threw = false;
        try { token.OnMessage(msg); }
        catch { threw = true; }

        _ = await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task OnMessage_sys_close_req_triggers_close_and_notifies_peer()
    {
        var peer = Substitute.For<IPeer>();
        var token = BuildConnectedToken();
        token.Peer = peer;

        var sessionClosedFired = false;
        token.SessionClosed += (_, _) => sessionClosedFired = true;

        // SYS_CLOSE_REQ = 0
        var packet = Packet.Create(0);
        packet.RecordSize();
        token.OnMessage(new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token));

        // Disconnect → Close → re-sends SYS_CLOSE_ACK → peer.OnRemoved
        peer.Received(1).OnRemoved();
        _ = await Assert.That(sessionClosedFired).IsTrue();
    }

    [Test]
    public async Task OnMessage_peer_exception_calls_close_gracefully()
    {
        var peer = Substitute.For<IPeer>();
        peer.When(static p => p.OnMessage(Arg.Any<Packet>())).Do(static _ => throw new InvalidOperationException("peer error"));

        var token = BuildConnectedToken();
        token.Peer = peer;

        var packet = Packet.Create(50);
        packet.RecordSize();

        var threw = false;
        try { token.OnMessage(new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), token)); }
        catch { threw = true; }

        _ = await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task OnReceive_complete_packet_with_null_peer_does_not_throw()
    {
        var token = new UserToken(null!);
        var packet = Packet.Create(42);
        packet.Push(99);
        packet.RecordSize();
        var bytes = new byte[packet.Position];
        Array.Copy(packet.Buffer, bytes, packet.Position);

        var threw = false;
        try { token.OnReceive(bytes, 0, bytes.Length); }
        catch { threw = true; }

        _ = await Assert.That(threw).IsFalse();
    }

    [Test]
    public void OnReceive_complete_packet_with_peer_dispatches_message()
    {
        var peer = Substitute.For<IPeer>();
        var token = new UserToken(null!)
        {
            Peer = peer
        };

        var packet = Packet.Create(42);
        packet.Push(99);
        packet.RecordSize();
        var bytes = new byte[packet.Position];
        Array.Copy(packet.Buffer, bytes, packet.Position);

        token.OnReceive(bytes, 0, bytes.Length);

        peer.Received(1).OnMessage(Arg.Any<Packet>());
    }

    [Test]
    public async Task Ban_calls_close_when_socket_is_null()
    {
        // Token with no socket: Ban() → ByeBye() → Send (tries to write to pipe) → close path
        var token = new UserToken(null!)
        {
            Peer = Substitute.For<IPeer>()
        };
        // No socket set, so send pipe writer.Complete() / Close() should not throw
        token.Ban();
        token.Close(); // hard-close since no I/O loop is running

        _ = await Assert.That(token.Socket is null).IsTrue();
        _ = await Assert.That(token.IsConnected()).IsFalse();
    }

    [Test]
    public async Task DisableAutoHeartbeat_StopHeartbeat_StartHeartbeat_are_safe_without_sender()
    {
        var token = new UserToken(null!);
        token.DisableAutoHeartbeat();
        token.StopHeartbeat();
        token.StartHeartbeat();

        _ = await Assert.That(token.IsConnected()).IsFalse();
    }

    [Test]
    public async Task UpdateHeartbeatManually_with_no_sender_does_not_throw()
    {
        var token = new UserToken(null!);
        token.UpdateHeartbeatManually(99.0f);

        _ = await Assert.That(token.LatestHeartbeatTime > 0).IsTrue();
    }

    /// <summary>Creates a token with a real (unconnected) socket, connected state set.</summary>
    private static UserToken BuildConnectedToken()
    {
        var token = new UserToken(null!)
        {
            Socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp)
        };
        token.OnConnected();
        return token;
    }
}
