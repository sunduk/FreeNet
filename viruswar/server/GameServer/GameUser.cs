using FreeNet;
using GameServer.UserState;

namespace GameServer;

/// <summary>
/// Represents a single session object.
/// </summary>
public class GameUser : IPeer
{
    private readonly UserToken _token;
    private readonly Dictionary<UserStateType, IUserState> _userStates;
    private IUserState? _currentUserState;

    public GameUser(UserToken token)
    {
        _token = token;
        _token.Peer = this;

        _userStates = new Dictionary<UserStateType, IUserState>
        {
            { UserStateType.Lobby, new UserLobbyState(this) },
            { UserStateType.Play, new UserPlayState(this) }
        };
        ChangeState(UserStateType.Lobby);
    }

    public GameRoom? BattleRoom { get; private set; }

    public Player? Player { get; private set; }

    public void ChangeState(UserStateType state) => _currentUserState = _userStates[state];

    public void Disconnect() => _token.Ban();

    public void EnterRoom(GameRoom room, byte player_index)
    {
        Player = new Player(this, player_index);
        BattleRoom = room;
        ChangeState(UserStateType.Play);
    }

    /// <inheritdoc/>
    public void OnMessage(Packet msg)
    {
        switch ((PROTOCOL)msg.ProtocolId)
        {
            case PROTOCOL.CONCURRENT_USERS:
                {
                    var count = Program.GetConcurrentUserCount();
                    var reply = Packet.Create((short)PROTOCOL.CONCURRENT_USERS);
                    reply.Push(count);
                    Send(reply);
                }

                return;
        }

        _currentUserState?.OnMessage(msg);
    }

    /// <inheritdoc/>
    public void OnRemoved()
    {
        Console.WriteLine("The client disconnected.");
        Program.RemoveUser(this);

        BattleRoom?.OnPlayerRemoved(Player);
    }

    /// <inheritdoc/>
    public void Send(Packet msg)
    {
        msg.RecordSize();

        // Copy before sending to the socket buffer.
        var clone = new byte[msg.Position];
        Array.Copy(msg.Buffer, clone, msg.Position);

        _token.Send(new ArraySegment<byte>(clone, 0, msg.Position));
    }
}
