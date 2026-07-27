namespace GameServer.UserState;

internal class UserPlayState(GameUser owner) : IUserState
{
    /// <summary>
    /// 플레이중 수신된 모든 메시지는 룸으로 넘겨서 처리한다.
    /// </summary>
    /// <param name="message">The message.</param>
    void IUserState.OnMessage(FreeNet.Packet message) =>
        owner.BattleRoom.OnReceive(owner.Player, message);
}
