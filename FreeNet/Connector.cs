using System;
using System.Net;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Connects to a server using endpoint information.
/// Create and use one instance per target server you want to connect to.
/// </summary>
public class Connector(NetworkService network_service)
{
    /// <summary>
    /// Socket used to connect to the remote server.
    /// </summary>
    private Socket _client;

    /// <summary>
    /// Callback delegate invoked when connection completes.
    /// </summary>
    /// <param name="token">The token.</param>
    public delegate void ConnectedHandler(UserToken token);

    /// <summary>
    /// Gets or sets the connected callback.
    /// </summary>
    /// <value>The connected callback.</value>
    public ConnectedHandler ConnectedCallback { get; set; } = null;

    /// <summary>
    /// Connects to the specified remote endpoint.
    /// </summary>
    /// <param name="remote_endpoint">The remote endpoint.</param>
    public void Connect(IPEndPoint remote_endpoint)
    {
        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        // Event args for asynchronous connect.
        SocketAsyncEventArgs event_arg = new();
        event_arg.Completed += OnConnectCompleted;
        event_arg.RemoteEndPoint = remote_endpoint;
        var pending = _client.ConnectAsync(event_arg);
        if (!pending)
        {
            OnConnectCompleted(this, event_arg);
        }
    }

    /// <summary>
    /// Called when the connect operation is completed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SocketAsyncEventArgs"/> instance containing the event data.</param>
    private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            //Console.WriteLine("Connect completd!");
            // Here, token represents the currently connected remote server.
            UserToken token = new(network_service.LogicEntry);

            // 1) Notify application code with the "connect completed" callback.
            // This must happen before starting receive handling in network code so the app is fully prepared.
            // If step 2 runs first and then step 1, packets received by network code may be missed by the app.
            ConnectedCallback?.Invoke(token);

            // 2) Prepare data receiving. Packet receive can start immediately after this call.
            // The application must already be ready to process packets passed from network code.
            network_service.OnConnectCompleted(_client, token);
        }
        else
        {
            // failed.
            Console.WriteLine(string.Format("Failed to connect. {0}", e.SocketError));
        }
    }
}
