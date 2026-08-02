namespace GameServer.UserState;

internal class UserPlayState(GameUser owner) : IUserState
{
    /// <summary>
    /// While playing, forward all received messages to the room for processing.
    /// </summary>
    /// <param name="message">The message.</param>
    void IUserState.OnMessage(FreeNet.Packet message) =>
        owner.BattleRoom?.OnReceive(owner.Player, message);
}
