using FreeNet;

namespace GameServer;

/// <summary>
/// 게임 서버의 실제 구현 클래스
/// </summary>
internal class GameServerImpl
{
    /// <summary>
    /// 매칭 대기 리스트.
    /// </summary>
    private readonly List<GameUser> _matchingWaitingUsers = [];

    /// <summary>
    /// 게임방을 관리하는 매니저.
    /// </summary>
    /// <value>The room manager.</value>
    public GameRoomManager RoomManager { get; private set; } = new();

    /// <summary>
    /// 유저로부터 매칭 요청이 왔을 때 호출됨.
    /// </summary>
    /// <param name="user">매칭을 신청한 유저 객체</param>
    public void MatchingReq(GameUser user)
    {
        // 대기 리스트에 중복 추가 되지 않도록 체크.
        if (_matchingWaitingUsers.Contains(user))
        {
            return;
        }

        // 매칭 대기 리스트에 추가.
        _matchingWaitingUsers.Add(user);

        // 2명이 모이면 매칭 성공.
        if (_matchingWaitingUsers.Count == 2)
        {
            // 게임 방 생성.
            RoomManager.CreateRoom(_matchingWaitingUsers[0], _matchingWaitingUsers[1]);

            // 매칭 대기 리스트 삭제.
            _matchingWaitingUsers.Clear();
        }
        else
        {
            // 매칭 인원이 모자를 경우 대기 메시지 전송.
            var msg = Packet.Create((short)PROTOCOL.ENTER_GAME_ROOM_ACK);
            user.Send(msg);
        }
    }

    /// <summary>
    /// 유저가 끊겼을 경우 매칭 대기 리스트에서 제거.
    /// </summary>
    /// <param name="user">연결이 끊긴 유저 객체</param>
    public void UserDisconnected(GameUser user)
    {
        if (_matchingWaitingUsers.Contains(user))
        {
            _ = _matchingWaitingUsers.Remove(user);
        }
    }
}
