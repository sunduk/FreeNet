using System;

namespace GameServer
{
    /// <summary>
    /// Protocol definition.
    /// Packets from server to client: S -> C
    /// Packets from client to server: C -> S
    /// </summary>
    public enum PROTOCOL : short
    {
        //-------------------------------------
        // Do not use values <= 0 in the game; they are reserved for termination codes!!
        //-------------------------------------
        BEGIN = 0,


        //-------------------------------------
        // Lobby protocol.
        //-------------------------------------
        // C -> S Request to enter game room.
        ENTER_GAME_ROOM_REQ = 1,

        // S -> C Response to game room entry request.
        ENTER_GAME_ROOM_ACK = 2,

        // S -> C Matching successful. Enter the room and start loading.
        START_LOADING = 3,

        // Concurrent user information request/response.
        CONCURRENT_USERS = 4,



        //-------------------------------------
        // Game protocol.
        //-------------------------------------
        // C -> S Game room resource loading is complete. OK to start the game.
        READY_TO_START = 10,

        // Game start.
        GAME_START = 11,

        // Turn start.
        START_PLAYER_TURN = 12,

        // C -> S Client movement request.
        MOVING_REQ = 13,

        // Player has moved.
        PLAYER_MOVED = 14,

        // C -> S Client turn animation is finished.
        TURN_FINISHED_REQ = 15,

        // Game over.
        GAME_OVER = 16,

        // Room removed.
        ROOM_REMOVED = 17,

        END
    }
}
