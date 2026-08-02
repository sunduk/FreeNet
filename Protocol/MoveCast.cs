using FreeNet;

namespace Protocol;

/// <summary>
/// Represents a cast movement request in a networked application, containing user ID, position, and rotation data.
/// Implements the <see cref="ProtocolMessage{MoveCast}"/>.
/// </summary>
/// <seealso cref="ProtocolMessage{MoveCast}"/>
public class MoveCast : ProtocolMessage<MoveCast>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveCast"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public MoveCast(Packet? message = null)
        : base(PacketProtocol.MOVE_CAST)
    {
        if (message is not null)
        {
            _ = FromPacket(message);
        }
    }

    /// <summary>
    /// Gets or sets the rotation.
    /// </summary>
    /// <value>The rotation.</value>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    /// <value>The user identifier.</value>
    public short UserID { get; set; }

    /// <summary>
    /// Gets or sets the x.
    /// </summary>
    /// <value>The x.</value>
    public float X { get; set; }

    /// <summary>
    /// Gets or sets the y.
    /// </summary>
    /// <value>The y.</value>
    public float Y { get; set; }

    /// <summary>
    /// Gets or sets the z.
    /// </summary>
    /// <value>The z.</value>
    public float Z { get; set; }

    /// <inheritdoc/>
    public override MoveCast FromPacket(Packet msg)
    {
        UserID = msg.PopInt16();
        X = msg.PopFloat();
        Y = msg.PopFloat();
        Z = msg.PopFloat();
        Rotation = msg.PopFloat();
        return this;
    }

    /// <inheritdoc/>
    public override Packet ToPacket()
    {
        var msg = Packet.Create(PacketProtocol.ToShort());
        msg.Push(UserID);
        msg.Push(X);
        msg.Push(Y);
        msg.Push(Z);
        msg.Push(Rotation);
        return msg;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{PacketProtocol} User ({UserID}) Pos ({X}, {Y}, {Z}) rot : {Rotation} deg";
}
