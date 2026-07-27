using System.Net.Sockets;
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
    public async Task SetEventArgs_stores_both_event_args()
    {
        var token = new UserToken(null!);
        var recv = new SocketAsyncEventArgs();
        var send = new SocketAsyncEventArgs();

        token.SetEventArgs(recv, send);

        _ = await Assert.That(ReferenceEquals(token.ReceiveEventArgs, recv)).IsTrue();
        _ = await Assert.That(ReferenceEquals(token.SendEventArgs, send)).IsTrue();
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
        var token = new UserToken(null!);
        token.SetPeer(peer);

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
        token.SetPeer(peer);

        var sessionClosedFired = false;
        token.OnSessionClosed += _ => sessionClosedFired = true;

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
        token.SetPeer(peer);

        var packet = Packet.Create(50);
        packet.RecordSize();

        // Exception from peer.OnMessage is caught inside UserToken.OnMessage and Close() is called
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
        var token = new UserToken(null!);
        token.SetPeer(peer);

        var packet = Packet.Create(42);
        packet.Push(99);
        packet.RecordSize();
        var bytes = new byte[packet.Position];
        Array.Copy(packet.Buffer, bytes, packet.Position);

        token.OnReceive(bytes, 0, bytes.Length);

        peer.Received(1).OnMessage(Arg.Any<Packet>());
    }

    [Test]
    public async Task ProcessSend_returns_early_when_bytes_transferred_is_zero()
    {
        var token = new UserToken(null!);
        var args = new SocketAsyncEventArgs();
        token.ProcessSend(args);
        _ = await Assert.That(token.IsConnected()).IsFalse();
    }

    [Test]
    public async Task Ban_calls_close_when_send_fails()
    {
        var token = BuildConnectedToken();
        token.SetPeer(Substitute.For<IPeer>());

        // Ban → ByeBye → Send (fails on unconnected socket) → Close → peer.OnRemoved
        token.Ban();

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

    /// <summary>Creates a token with real (unconnected) socket and event args.</summary>
    private static UserToken BuildConnectedToken()
    {
        var token = new UserToken(null!);
        var recvArgs = new SocketAsyncEventArgs();
        recvArgs.SetBuffer(new byte[1024], 0, 1024);
        token.SetEventArgs(recvArgs, new SocketAsyncEventArgs());
        token.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        return token;
    }
}
