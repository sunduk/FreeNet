using FreeNet;

namespace GameServer.UserState;

/// <summary>
/// The IUserState interface defines the contract for user states in the game server. Each user state must implement the
/// OnMessage method to handle incoming messages from the client.
/// </summary>
internal interface IUserState
{
    /// <summary>
    /// Called when a message is received from the client.
    /// </summary>
    /// <param name="message">The message.</param>
    void OnMessage(Packet message);
}