using System.Reflection;
using NSubstitute;

namespace FreeNet.Tests;

public class ServiceLifecycleTests
{
    [Test]
    public async Task ServerUserManager_add_exists_remove_and_count_work()
    {
        var manager = new ServerUserManager();
        var token = new UserToken(null!);

        manager.Add(token);

        _ = await Assert.That(manager.Exists(token)).IsTrue();
        _ = await Assert.That(manager.GetTotalCount()).IsEqualTo(1);

        manager.Remove(token);

        _ = await Assert.That(manager.Exists(token)).IsFalse();
        _ = await Assert.That(manager.GetTotalCount()).IsEqualTo(0);
    }

    [Test]
    public async Task ServerUserManager_heartbeat_check_disconnects_stale_sessions()
    {
        var manager = new ServerUserManager();
        var token = new UserToken(null!)
        {
            Socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp)
        };

        SetProperty(token, nameof(UserToken.LatestHeartbeatTime), 0L);

        manager.Add(token);
        SetField(manager, "_heartbeatDuration", 1L);

        InvokeInstance(manager, "CheckHeartbeat", [null!]);

        _ = await Assert.That(token.Socket is null).IsTrue();
    }

    [Test]
    public async Task LogicMessageEntry_dispatches_only_for_registered_users()
    {
        var service = new NetworkService();
        var entry = new LogicMessageEntry(service);

        var inListToken = new UserToken(null!);
        var skippedToken = new UserToken(null!);
        var inListPeer = Substitute.For<IPeer>();
        var skippedPeer = Substitute.For<IPeer>();

        inListToken.Peer=inListPeer;
        skippedToken.Peer=skippedPeer;
        service.Usermanager.Add(inListToken);

        var firstBytes = CreateMessageBytes(100);
        var secondBytes = CreateMessageBytes(200);
        var queue = new Queue<Packet>(
        [
            new Packet(new ArraySegment<byte>(firstBytes, 0, firstBytes.Length), inListToken),
            new Packet(new ArraySegment<byte>(secondBytes, 0, secondBytes.Length), skippedToken)
        ]);

        InvokeInstance(entry, "DispatchAll", [queue]);

        inListPeer.Received(1).OnMessage(Arg.Any<Packet>());
        skippedPeer.DidNotReceive().OnMessage(Arg.Any<Packet>());
        _ = await Assert.That(queue.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NetworkService_onsessionclosed_removes_user()
    {
        var service = new NetworkService();

        var token = new UserToken(null!);
        service.Usermanager.Add(token);

        InvokeInstance(service, "OnSessionClosed", [null!, new SessionEventArgs(token)]);

        _ = await Assert.That(service.Usermanager.Exists(token)).IsFalse();
    }

    [Test]
    public async Task HeartbeatSender_update_under_interval_does_not_send()
    {
        var token = new UserToken(null!);
        var sender = new HeartbeatSender(token, interval: 3);

        sender.Update(1.5f);

        _ = await Assert.That(token.Socket is null).IsTrue();
    }

    [Test]
    public async Task Peer_onmessage_parses_protocol_one_payload_without_throwing()
    {
        var packet = Packet.Create(1);
        packet.Push(55);
        packet.Push("hello");
        packet.RecordSize();

        var threw = false;
        try
        {
            Peer.OnMessage(new Const<byte[]>(packet.Buffer));
        }
        catch
        {
            threw = true;
        }

        _ = await Assert.That(threw).IsFalse();
    }

    private static void InvokeInstance(object target, string methodName, object[] arguments)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException($"Missing method: {methodName}");
        _ = method.Invoke(target, arguments);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException($"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new InvalidOperationException($"Missing property: {propertyName}");
        property.SetValue(target, value);
    }

    private static byte[] CreateMessageBytes(short protocol)
    {
        var packet = Packet.Create(protocol);
        packet.RecordSize();
        var bytes = new byte[packet.Position];
        Array.Copy(packet.Buffer, bytes, packet.Position);
        return bytes;
    }
}
