using FreeNet;

namespace Protocol;

/// <summary>
/// Represents user information.
/// Implements the <see cref="Protocol.AProtocolMessage{Protocol.SCUserInfo}" />
/// </summary>
/// <seealso cref="Protocol.AProtocolMessage{Protocol.SCUserInfo}" />
public class SCUserInfo : AProtocolMessage<SCUserInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SCUserInfo"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public SCUserInfo(Packet? message = null)
        : base(PacketProtocol.USER_INFO)
    {
        if (message is not null)
        {
            _ = FromPacket(message);
        }
    }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    /// <value>The user identifier.</value>
    public short UserID { get; set; }

    /// <inheritdoc/>
    public override SCUserInfo FromPacket(Packet msg)
    {
        UserID = msg.PopInt16();
        return this;
    }

    /// <inheritdoc/>
    public override Packet ToPacket()
    {
        var ret = Packet.Create(PacketProtocol.ToShort());
        ret.Push(UserID);
        return ret;
    }
}
