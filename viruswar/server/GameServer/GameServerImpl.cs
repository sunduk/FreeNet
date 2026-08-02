using FreeNet;

namespace GameServer;

/// <summary>
/// Concrete implementation of the game server.
/// </summary>
internal class GameServerImpl
{
    /// <summary>
    /// Matchmaking waiting list.
    /// </summary>
    private readonly List<GameUser> _matchingWaitingUsers = [];

    /// <summary>
    /// Manager that controls game rooms.
    /// </summary>
    /// <value>The room manager.</value>
    public GameRoomManager RoomManager { get; private set; } = new();

    /// <summary>
    /// Called when a user requests matchmaking.
    /// </summary>
    /// <param name="user">User object that requested matching.</param>
    public void MatchingReq(GameUser user)
    {
        // Prevent duplicate insertion into the waiting list.
        if (_matchingWaitingUsers.Contains(user))
        {
            return;
        }

        // Add to matchmaking waiting list.
        _matchingWaitingUsers.Add(user);

        // Match succeeds when two users are waiting.
        if (_matchingWaitingUsers.Count == 2)
        {
            // Create a game room.
            RoomManager.CreateRoom(_matchingWaitingUsers[0], _matchingWaitingUsers[1]);

            // Clear matchmaking waiting list.
            _matchingWaitingUsers.Clear();
        }
        else
        {
            // Send a waiting message if not enough users are matched yet.
            var msg = Packet.Create((short)PROTOCOL.ENTER_GAME_ROOM_ACK);
            user.Send(msg);
        }
    }

    /// <summary>
    /// Removes a disconnected user from the matchmaking waiting list.
    /// </summary>
    /// <param name="user">Disconnected user object.</param>
    public void UserDisconnected(GameUser user)
    {
        if (_matchingWaitingUsers.Contains(user))
        {
            _ = _matchingWaitingUsers.Remove(user);
        }
    }
}
