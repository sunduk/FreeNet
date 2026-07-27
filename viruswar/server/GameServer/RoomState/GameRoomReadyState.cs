using FreeNet;
using GameServer.State;

namespace GameServer.RoomState;

/// <summary>
/// Represents the state of a game room when it is ready to start. Implements the <see cref="IState"/>
/// </summary>
/// <seealso cref="IState"/>
internal class GameRoomReadyState : IState
{
    private readonly GameRoom _room;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameRoomReadyState"/> class.
    /// </summary>
    /// <param name="room">The room.</param>
    public GameRoomReadyState(GameRoom room)
    {
        _room = room;

        _room.StateManager.RegisterMessageHandler(this, PROTOCOL.READY_TO_START, OnReadyReq);
    }

    /// <inheritdoc/>
    public void OnEnter()
    {
    }

    /// <inheritdoc/>
    public void OnExit()
    {
    }

    private void OnReadyReq(Player sender, Packet message)
    {
        if (_room.AllReceived(PROTOCOL.READY_TO_START))
        {
            _room.StateManager.ChangeState(GameRoom.STATE.PLAY);
        }
    }
}
