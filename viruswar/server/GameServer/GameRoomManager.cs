namespace GameServer;

/// <summary>
/// Room manager that manages game rooms.
/// </summary>
public class GameRoomManager
{
    private readonly List<GameRoom> _rooms = [];

    /// <summary>
    /// Creates a game room for users who requested matching.
    /// </summary>
    /// <param name="user1">First user who requested matching.</param>
    /// <param name="user2">Second user who requested matching.</param>
    public void CreateRoom(GameUser user1, GameUser user2)
    {
        // Create the game room and let players enter.
        var battleroom = new GameRoom(this);
        _rooms.Add(battleroom);

        user1.EnterRoom(battleroom, 0);
        user2.EnterRoom(battleroom, 1);

        battleroom.EnterGameRoom(user1.Player, user2.Player);
    }

    /// <summary>
    /// Removes a game room.
    /// </summary>
    /// <param name="room">Game room to remove.</param>
    public void RemoveRoom(GameRoom room)
    {
        room.Destroy();
        _ = _rooms.Remove(room);
    }
}
