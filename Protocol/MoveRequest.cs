using FreeNet;

namespace Protocol;

/// <summary>
/// Represents a request to move an entity in a networked application, containing position and rotation data. Implements
/// the <see cref="ProtocolMessage{MoveReq}"/>
/// </summary>
/// <seealso cref="ProtocolMessage{MoveReq}"/>
public class MoveRequest : ProtocolMessage<MoveRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveRequest"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public MoveRequest(Packet? message = null)
        : base(PacketProtocol.MOVE_REQ)
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
    public override MoveRequest FromPacket(Packet msg)
    {
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

        msg.Push(X);
        msg.Push(Y);
        msg.Push(Z);
        msg.Push(Rotation);

        return msg;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{PacketProtocol} Pos ({X}, {Y}, {Z}) rot : {Rotation} deg";
}
