namespace GameServer.UserState;

internal class UserLobbyState(GameUser owner) : IUserState
{
    /// <inheritdoc/>
    public void OnMessage(FreeNet.Packet msg)
    {
        var protocol = (PROTOCOL)msg.PopProtocolId();
        Console.WriteLine($"protocol id {protocol}");
        switch (protocol)
        {
            case PROTOCOL.ENTER_GAME_ROOM_REQ:
                Program.GameMain.MatchingReq(owner);
                break;
        }
    }
}
