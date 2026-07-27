namespace GameServer;

/// <summary>
/// 게임방들을 관리하는 룸매니저.
/// </summary>
public class GameRoomManager
{
    private readonly List<GameRoom> _rooms = [];

    /// <summary>
    /// 매칭을 요청한 유저들을 넘겨 받아 게임 방을 생성한다.
    /// </summary>
    /// <param name="user1">매칭을 요청한 첫 번째 유저 객체</param>
    /// <param name="user2">매칭을 요청한 두 번째 유저 객체</param>
    public void CreateRoom(GameUser user1, GameUser user2)
    {
        // 게임 방을 생성하여 입장 시킴.
        var battleroom = new GameRoom(this);
        _rooms.Add(battleroom);

        user1.EnterRoom(battleroom, 0);
        user2.EnterRoom(battleroom, 1);

        battleroom.EnterGameRoom(user1.Player, user2.Player);
    }

    /// <summary>
    /// 게임 방을 제거한다.
    /// </summary>
    /// <param name="room">제거할 게임 방 객체</param>
    public void RemoveRoom(GameRoom room)
    {
        room.Destroy();
        _ = _rooms.Remove(room);
    }
}
