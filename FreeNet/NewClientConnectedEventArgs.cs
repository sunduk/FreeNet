using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Event arguments for the new client connection event. Contains the client socket and an associated token. This class
/// cannot be inherited. Implements the <see cref="System.EventArgs"/>
/// </summary>
/// <param name="clientSocket">The client socket.</param>
/// <param name="token">The token.</param>
/// <seealso cref="System.EventArgs"/>
public sealed class NewClientConnectedEventArgs(Socket clientSocket, object? token) : EventArgs
{
    /// <summary>
    /// Gets the client socket.
    /// </summary>
    /// <value>The client socket.</value>
    public Socket ClientSocket { get; } = clientSocket;

    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <value>The token.</value>
    public object? Token { get; } = token;
}
