using System;

namespace FreeNet;

/// <summary>
/// Represents a single session object. This class is closer to logic handling than CUserToken.
/// </summary>
internal class Peer
{
    /// <summary>
    /// Called when one complete packet has been assembled from socket buffer data.
    /// Call flow: .NET Socket ReceiveAsync -&gt; CUserToken.on_receive -&gt; CPeer.on_message.
    /// For TCP packet ordering: this method runs on the .NET thread pool, so the calling thread is not fixed.
    /// However, for a single CPeer instance, the next packet is processed only after this method completes,
    /// so packet order from a client is preserved. Be careful with shared resources or other CPeer instances;
    /// use proper locking to avoid multithreading issues. For game packets, queuing under lock and processing
    /// on a single thread is often simpler.
    /// </summary>
    /// <param name="buffer">
    /// References the CUserToken buffer copied from the socket buffer. When this method returns, the buffer is
    /// cleared and reused for the next packet, so extract all required data before returning.
    /// </param>
    public static void OnMessage(Const<byte[]> buffer)
    {
        Packet msg = new(buffer.Value, null);
        var protocolId = msg.PopInt16();
        switch (protocolId)
        {
            case 1:
                var number = msg.PopInt32();
                var text = msg.PopString();

                Console.WriteLine(
                    string.Format(
                        "[{0}] [received] {1} : {2}, {3}",
                        Environment.CurrentManagedThreadId,
                        protocolId,
                        number,
                        text));

                break;
        }
    }
}
