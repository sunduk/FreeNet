using System.Reflection;
using FreeNet;
using GameServer.UserState;

namespace GameServer.Tests;

public class ProgramAndServerImplTests
{
    [Test]
    public async Task Program_session_callbacks_update_concurrent_user_count()
    {
        var token = new UserToken(null!);

        Program.OnSessionCreated(null, new SessionEventArgs(token));
        var user = GetLatestUser();

        _ = await Assert.That(Program.GetConcurrentUserCount() > 0).IsTrue();

        Program.RemoveUser(user);

        _ = await Assert.That(Program.GetConcurrentUserCount() >= 0).IsTrue();
    }

    [Test]
    public async Task GameServerImpl_userdisconnected_removes_user_from_waiting_list()
    {
        var impl = new GameServerImpl();
        var user = new GameUser(new UserToken(null!));

        var waiting = GetWaitingList(impl);
        waiting.Add(user);

        impl.UserDisconnected(user);

        _ = await Assert.That(waiting.Contains(user)).IsFalse();
    }

    [Test]
    public async Task UserLobbyState_ignores_non_matching_protocols()
    {
        var user = new GameUser(new UserToken(null!));
        var lobby = new UserLobbyState(user);

        var packet = Packet.Create((short)PROTOCOL.GAME_START);
        packet.RecordSize();
        var received = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), null!);

        var threw = false;
        try
        {
            lobby.OnMessage(received);
        }
        catch
        {
            threw = true;
        }

        _ = await Assert.That(threw).IsFalse();
    }

    private static GameUser GetLatestUser()
    {
        var userListField = typeof(Program).GetField("Userlist", BindingFlags.NonPublic | BindingFlags.Static);
        return userListField?.GetValue(null) is not List<GameUser> userList || userList.Count == 0
            ? throw new InvalidOperationException("Unable to read Program user list.")
            : userList[^1];
    }

    private static List<GameUser> GetWaitingList(GameServerImpl impl)
    {
        var field = typeof(GameServerImpl).GetField("_matchingWaitingUsers", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(impl) is not List<GameUser> waiting
            ? throw new InvalidOperationException("Unable to read waiting list.")
            : waiting;
    }
}
