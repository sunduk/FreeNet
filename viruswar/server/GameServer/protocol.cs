namespace GameServer;

/// <summary>
/// Protocol definitions.
/// Packets from server to client: S -&gt; C. Packets from client to server: C -&gt; S.
/// </summary>
public enum PROTOCOL : short
{
    //-------------------------------------
    // Values <= 0 are reserved as termination/system codes. Do not use for game protocols.
    //-------------------------------------
    BEGIN = 0,

    //-------------------------------------
    // Lobby protocols.
    //-------------------------------------
    // C -> S game room enter request.
    ENTER_GAME_ROOM_REQ = 1,

    // S -> C response to game room enter request.
    ENTER_GAME_ROOM_ACK = 2,

    // S -> C matching succeeded. Enter room and start loading.
    START_LOADING = 3,

    // Concurrent user count request/response.
    CONCURRENT_USERS = 4,

    //-------------------------------------
    // Game protocols.
    //-------------------------------------
    // C -> S game-room resource loading complete. Ready to start game.
    READY_TO_START = 10,

    // Game start.
    GAME_START = 11,

    // Start turn.
    START_PLAYER_TURN = 12,

    // Client move request.
    MOVING_REQ = 13,

    // Notify that a player moved.
    PLAYER_MOVED = 14,

    // Notify that the client's turn animation is finished.
    TURN_FINISHED_REQ = 15,

    // Game over.
    GAME_OVER = 16,

    // Room removed.
    ROOM_REMOVED = 17,

    END
}
