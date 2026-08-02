using System.Net;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Connects to a server using endpoint information. Create and use one instance per target server you want to connect
/// to.
/// </summary>
public class Connector(NetworkService networkService)
{
    /// <summary>
    /// Socket used to connect to the remote server.
    /// </summary>
    private Socket? _client;

    /// <summary>
    /// Gets or sets the connected callback.
    /// </summary>
    /// <value>The connected callback.</value>
    public event EventHandler<ConnectedEventArgs>? Connected;

    /// <summary>
    /// Connects to the specified remote endpoint asynchronously.
    /// </summary>
    /// <param name="remoteEndpoint">The remote endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ConnectAsync(IPEndPoint remoteEndpoint, CancellationToken cancellationToken = default)
    {
        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        try
        {
            await _client.ConnectAsync(remoteEndpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Failed to connect. {ex.SocketErrorCode}");
            return;
        }

        if (_client is null)
        {
            return;
        }

        // Here, token represents the currently connected remote server.
        UserToken token = new(networkService.LogicEntry);

        // 1) Notify application code with the "connect completed" callback. This must happen before
        // starting receive handling in network code so the app is fully prepared. If step 2 runs
        // first and then step 1, packets received by network code may be missed by the app.
        Connected?.Invoke(this, new ConnectedEventArgs(token));

        // 2) Prepare data receiving. Packet receive can start immediately after this call. The
        // application must already be ready to process packets passed from network code.
        networkService.OnConnectCompleted(_client, token);
    }
}
