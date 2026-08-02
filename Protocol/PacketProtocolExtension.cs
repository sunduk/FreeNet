namespace Protocol;

/// <summary>
/// Extension methods for the PacketProtocol enum to facilitate conversion between short and PacketProtocol types.
/// </summary>
public static class PacketProtocolExtension
{
    /// <summary>
    /// Converts to protocol.
    /// </summary>
    /// <param name="protocol">The protocol.</param>
    /// <returns>PacketProtocol.</returns>
    public static PacketProtocol ToProtocol(this short protocol) => (PacketProtocol)protocol;

    /// <summary>
    /// Converts to short.
    /// </summary>
    /// <param name="protocol">The protocol.</param>
    /// <returns>short.</returns>
    public static short ToShort(this PacketProtocol protocol) => (short)protocol;
}
