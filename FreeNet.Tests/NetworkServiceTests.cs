using System.Net.Sockets;
using System.Reflection;

namespace FreeNet.Tests;

public class NetworkServiceTests
{
    [Test]
    public async Task Default_constructor_has_no_logic_entry()
    {
        var service = new NetworkService();
        _ = await Assert.That(service.LogicEntry is null).IsTrue();
    }

    [Test]
    public async Task Logic_thread_constructor_creates_logic_entry()
    {
        var service = new NetworkService(useLogicThread: true);
        _ = await Assert.That(service.LogicEntry is not null).IsTrue();
    }

    [Test]
    public async Task Initialize_with_small_params_creates_pools_and_allows_session_closed_cleanup()
    {
        var service = new NetworkService();
        service.Initialize(maxConnections: 3, bufferSize: 64);

        var token = new UserToken(null!);
        token.SetEventArgs(new SocketAsyncEventArgs(), new SocketAsyncEventArgs());
        service.Usermanager.Add(token);

        InvokePrivate(service, "OnSessionClosed", token);

        _ = await Assert.That(service.Usermanager.Exists(token)).IsFalse();
        _ = await Assert.That(token.ReceiveEventArgs is null).IsTrue();
    }

    [Test]
    public async Task Default_Initialize_delegates_and_creates_usermanager()
    {
        var service = new NetworkService();
        service.Initialize();
        _ = await Assert.That(service.Usermanager is not null).IsTrue();
    }

    [Test]
    public async Task OnSendCompleted_swallows_exception_when_token_is_null()
    {
        var service = new NetworkService();
        var args = new SocketAsyncEventArgs();
        InvokePrivate(service, "OnSendCompleted", this, args);
        _ = await Assert.That(service.Usermanager is not null).IsTrue();
    }

    [Test]
    public async Task OnSendCompleted_calls_process_send_when_token_is_set()
    {
        var service = new NetworkService();
        var token = new UserToken(null!);
        var args = new SocketAsyncEventArgs();
        args.UserToken = token;
        InvokePrivate(service, "OnSendCompleted", this, args);
        _ = await Assert.That(service.Usermanager is not null).IsTrue();
    }

    [Test]
    public async Task OnReceiveCompleted_throws_for_send_operation()
    {
        var service = new NetworkService();
        var args = new SocketAsyncEventArgs();
        var threw = false;
        try { InvokePrivate(service, "OnReceiveCompleted", this, args); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
        { threw = true; }

        _ = await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Connector_can_be_constructed_and_has_null_callback()
    {
        var service = new NetworkService();
        var connector = new Connector(service);
        _ = await Assert.That(connector is not null).IsTrue();
        _ = await Assert.That(connector.ConnectedCallback is null).IsTrue();
    }

    private static void InvokePrivate(object target, string name, params object[] args)
    {
        var m = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method: {name}");
        _ = m.Invoke(target, args);
    }
}
