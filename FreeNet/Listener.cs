using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FreeNet;

/// <summary>
/// Listener class is responsible for accepting new client connections on a specified host and port. It uses
/// asynchronous socket operations to handle incoming connections efficiently. The class provides a callback mechanism
/// to notify when a new client has connected, allowing for separation of socket handling and content implementation.
/// </summary>
internal class Listener
{
    /// <summary>
    /// The callback on new client
    /// </summary>
    public event EventHandler<NewClientConnectedEventArgs>? NewClientConnected;

    /// <summary>
    /// The listen socket
    /// </summary>
    private Socket? _listenSocket;

    /// <summary>
    /// Cancellation source for the accept loop.
    /// </summary>
    private CancellationTokenSource? _stopAccepting;

    /// <summary>
    /// Starts the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="port">The port.</param>
    /// <param name="backlog">The backlog.</param>
    public void Start(string host, int port, int backlog)
    {
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
        IPEndPoint endpoint = new(address, port);
        try
        {
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(backlog);
            _stopAccepting = new CancellationTokenSource();
            _ = DoListenAsync(_stopAccepting.Token);
        }
        catch (SocketException)
        {
            _listenSocket.Dispose();
            _listenSocket = null;
            throw;
        }
    }

    /// <summary>
    /// Stops accepting clients.
    /// </summary>
    public void Stop()
    {
        _stopAccepting?.Cancel();
        _listenSocket?.Close();
    }

    /// <summary>
    /// Accepts clients in a loop using the modern Socket.AcceptAsync API.
    /// </summary>
    private async Task DoListenAsync(CancellationToken cancellationToken)
    {
        if (_listenSocket is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _listenSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                clientSocket.NoDelay = true;
                NewClientConnected?.Invoke(this, new NewClientConnectedEventArgs(clientSocket, null));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to accept client. {ex.SocketErrorCode}");
            }
        }
    }
}
