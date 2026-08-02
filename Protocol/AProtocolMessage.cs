using FreeNet;

namespace Protocol;

/// <summary>
/// Represents an abstract base class for protocol messages.
/// </summary>
/// <typeparam name="T">The type of the protocol message that inherits from this base class.</typeparam>
/// <param name="protocol">The protocol.</param>
public abstract class AProtocolMessage<T>(PacketProtocol protocol) where T : AProtocolMessage<T>
{
    /// <summary>
    /// Gets the packet protocol.
    /// </summary>
    /// <value>The packet protocol.</value>
    protected PacketProtocol PacketProtocol { get; } = protocol;

    /// <summary>
    /// Froms the packet.
    /// </summary>
    /// <param name="msg">The MSG.</param>
    /// <returns>T.</returns>
    public abstract T FromPacket(Packet msg);

    /// <summary>
    /// Converts to packet.
    /// </summary>
    /// <returns>Packet.</returns>
    public abstract Packet ToPacket();
}
