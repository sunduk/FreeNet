using FreeNet;

namespace GameServer.RoomState;

/// <summary>
/// Interface for room state management in a game server.
/// </summary>
public interface IRoomState
{
    /// <summary>
    /// Called when a message is received from a player in the room.
    /// </summary>
    /// <param name="protocol">The protocol.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="message">The message.</param>
    void OnReceive(PROTOCOL protocol, Player owner, Packet message);
}
