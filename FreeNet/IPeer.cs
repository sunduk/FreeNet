namespace FreeNet;

/// <summary>
/// A shared session contract used by both server and client.
/// On the server, it represents one client object and is created/returned from CNetworkService session callbacks.
/// Whether to pool these objects is up to the user implementation.
/// On the client, it represents the connected server object.
/// </summary>
public interface IPeer
{
    // Removed.
    //void OnMessage(ArraySegment<byte> buffer);

    // Removed.
    //void ProcessUserOperation(CPacket msg);

    void Disconnect();

    /// <summary>
    /// In CNetworkService.Initialize: if use_logicthread is true, this is called directly from the I/O thread;
    /// if false, it is called from the logic thread. The logic thread runs as a single thread.
    /// </summary>
    /// <param name="message">The packet.</param>
    void OnMessage(Packet message);

    /// <summary>
    /// Called when the remote connection is closed. Data can no longer be sent after this is called.
    /// </summary>
    void OnRemoved();

    /// <summary>
    /// Sends the specified packet.
    /// </summary>
    /// <param name="message">The packet.</param>
    void Send(Packet message);
}
