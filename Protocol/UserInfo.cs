using FreeNet;

namespace Protocol;

/// <summary>
/// Represents user information. Implements the <see cref="ProtocolMessage{UserInfo}"/>
/// </summary>
/// <seealso cref="ProtocolMessage{UserInfo}"/>
public class UserInfo : ProtocolMessage<UserInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfo"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public UserInfo(Packet? message = null)
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
    public override UserInfo FromPacket(Packet msg)
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
