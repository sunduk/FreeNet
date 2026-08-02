namespace Protocol;

/// <summary>
/// PacketProtocol defines the protocol identifiers for different packet types used in the application.
/// </summary>
public enum PacketProtocol : short
{
    /// <summary>
    /// The beginning of the protocol identifiers.
    /// </summary>
    BEGIN = 0,

    /// <summary>
    /// Protocol identifier for chat message request.
    /// </summary>
    CHAT_MSG_REQ = 1,

    /// <summary>
    /// Protocol identifier for chat message acknowledgment.
    /// </summary>
    CHAT_MSG_ACK = 2,

    /// <summary>
    /// Protocol identifier for move request.
    /// </summary>
    MOVE_REQ = 3,

    /// <summary>
    /// Protocol identifier for move cast.
    /// </summary>
    MOVE_CAST = 4,

    /// <summary>
    /// Protocol identifier for user information.
    /// </summary>
    USER_INFO = 5,

    /// <summary>
    /// The end of the protocol identifiers.
    /// </summary>
    END,
}
