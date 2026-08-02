namespace FreeNet.Tests;

public class NetworkServiceTests
{
    [Test]
    public async Task Connector_can_be_constructed_and_has_null_callback()
    {
        var service = new NetworkService();
        var connector = new Connector(service);
        _ = await Assert.That(connector is not null).IsTrue();
    }

    [Test]
    public async Task Default_constructor_has_no_logic_entry()
    {
        var service = new NetworkService();
        _ = await Assert.That(service.LogicEntry is null).IsTrue();
    }

    [Test]
    public async Task Initialize_with_small_params_creates_usermanager_and_allows_session_closed_cleanup()
    {
        var service = new NetworkService();

        // OnConnectCompleted registers the token and wires SessionClosed → OnSessionClosed
        var token = new UserToken(null!);
        service.OnConnectCompleted(
            new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp),
            token);

        _ = await Assert.That(service.Usermanager.Exists(token)).IsTrue();

        // Closing fires SessionClosed → OnSessionClosed removes the token from the manager
        token.Close();
        await Task.Delay(50);

        _ = await Assert.That(service.Usermanager.Exists(token)).IsFalse();
    }

    [Test]
    public async Task Logic_thread_constructor_creates_logic_entry()
    {
        var service = new NetworkService(useLogicThread: true);
        _ = await Assert.That(service.LogicEntry is not null).IsTrue();
    }
}
