using System.Collections.Generic;

namespace FreeNet;

/// <summary>
/// Interface for a logic queue that handles packets in a thread-safe manner.
/// </summary>
public interface ILogicQueue
{
    /// <summary>
    /// Enqueues the specified <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The packet.</param>
    void Enqueue(Packet message);

    /// <summary>
    /// Gets all the packets in the queue.
    /// </summary>
    /// <returns>A queue containing all the packets.</returns>
    Queue<Packet> GetAll();
}