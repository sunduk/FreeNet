namespace FreeNet;

/// <summary>
/// Provides data for session lifecycle events.
/// </summary>
/// <param name="token">The session token.</param>
public sealed class SessionEventArgs(UserToken token) : EventArgs
{
    /// <summary>
    /// Gets the session token.
    /// </summary>
    public UserToken Token { get; } = token;
}
