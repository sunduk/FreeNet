using System.Collections.Generic;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Uses two queues by swapping references.
/// The I/O thread keeps enqueuing to the input queue, and the logic thread swaps queues and processes
/// the accumulated packets from the output queue. Reference:
/// http://roadster.egloos.com/m/4199854
/// </summary>
internal class DoubleBufferingQueue : ILogicQueue
{
    private readonly Lock _cs_write;

    // Queues that store actual data.
    private readonly Queue<Packet> _queue1;

    private readonly Queue<Packet> _queue2;

    // References to each queue.
    private Queue<Packet> _refInput;

    private Queue<Packet> _refOutput;

    public DoubleBufferingQueue()
    {
        // Initial mapping keeps queue and reference in a 1:1 match. refInput->queue1, refOutput->queue2
        _queue1 = new Queue<Packet>();
        _queue2 = new Queue<Packet>();
        _refInput = _queue1;
        _refOutput = _queue2;

        _cs_write = new Lock();
    }

    /// <summary>
    /// Stores packets received from the I/O thread.
    /// </summary>
    /// <param name="msg">The packet to store.</param>
    void ILogicQueue.Enqueue(Packet msg)
    {
        using var scope = _cs_write.EnterScope();
        _refInput.Enqueue(msg);
    }

    Queue<Packet> ILogicQueue.GetAll()
    {
        Swap();
        return _refOutput;
    }

    /// <summary>
    /// Swaps the input and output queues.
    /// </summary>
    private void Swap()
    {
        using var scope = _cs_write.EnterScope();
        (_refOutput, _refInput) = (_refInput, _refOutput);
    }
}
