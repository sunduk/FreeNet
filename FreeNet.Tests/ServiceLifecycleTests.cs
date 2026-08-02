using System.Net.Sockets;
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
        var token = new UserToken(null!);
        token.SetEventArgs(new SocketAsyncEventArgs(), new SocketAsyncEventArgs());
        token.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

        inListToken.SetPeer(inListPeer);
        skippedToken.SetPeer(skippedPeer);
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
    public async Task NetworkService_onreceivecompleted_throws_for_non_receive_operation()
    {
        var service = new NetworkService();
        var args = new SocketAsyncEventArgs();

        var threw = false;
        try
        {
            InvokeInstance(service, "OnReceiveCompleted", [this, args]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
        {
            threw = true;
        }

        _ = await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task NetworkService_onsessionclosed_removes_user_and_recycles_event_args()
    {
        var service = new NetworkService();
        service.Initialize(2, 128);

        var token = new UserToken(null!);
        token.SetEventArgs(new SocketAsyncEventArgs(), new SocketAsyncEventArgs());
        service.Usermanager.Add(token);

        InvokeInstance(service, "OnSessionClosed", [token]);

        _ = await Assert.That(service.Usermanager.Exists(token)).IsFalse();
        _ = await Assert.That(token.ReceiveEventArgs is null).IsTrue();
        _ = await Assert.That(token.SendEventArgs is null).IsTrue();
    }

    [Test]
    public async Task Listener_accept_callback_is_forwarded_to_registered_handler()
    {
        var listener = new Listener();
        var accepted = false;
        listener.CallbackOnNewClient = (socket, _) => accepted = socket is not null;

        SetField(listener, "_flowControlEvent", new AutoResetEvent(false));

        var acceptArgs = new SocketAsyncEventArgs
        {
            AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        };

        InvokeInstance(listener, "OnAcceptCompleted", [null!, acceptArgs]);

        _ = await Assert.That(accepted).IsTrue();
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
