namespace FreeNet;

/// <summary>
/// Provides data for the <see cref="Connector.Connected"/> event. Implements the <see cref="EventArgs"/>
/// </summary>
/// <param name="token">The token.</param>
/// <seealso cref="EventArgs"/>
public class ConnectedEventArgs(UserToken token) : EventArgs
{
    /// <summary>
    /// Gets the user token.
    /// </summary>
    /// <value>The user token.</value>
    public UserToken Token { get; } = token;
}
