using System.Collections.Generic;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Uses two queues by swapping references. The I/O thread keeps enqueuing to the input queue, and the logic thread
/// swaps queues and processes the accumulated packets from the output queue. Reference:
/// http://roadster.egloos.com/m/4199854
/// </summary>
internal class DoubleBufferingQueue : ILogicQueue
{
    /// <summary>
    /// The first queue used for storing packets.
    /// </summary>
    private readonly Queue<Packet> _queue1;

    /// <summary>
    /// The second queue used for storing packets.
    /// </summary>
    private readonly Queue<Packet> _queue2;

    /// <summary>
    /// The lock used to synchronize access to the queues.
    /// </summary>
    private readonly Lock _writeLock;

    /// <summary>
    /// The reference input
    /// </summary>
    private Queue<Packet> _refInput;

    /// <summary>
    /// The reference output
    /// </summary>
    private Queue<Packet> _refOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleBufferingQueue"/> class.
    /// </summary>
    public DoubleBufferingQueue()
    {
        // Initial mapping keeps queue and reference in a 1:1 match. refInput->queue1, refOutput->queue2
        _queue1 = new Queue<Packet>();
        _queue2 = new Queue<Packet>();
        _refInput = _queue1;
        _refOutput = _queue2;

        _writeLock = new Lock();
    }

    /// <inheritdoc/>
    public void Enqueue(Packet msg)
    {
        using var scope = _writeLock.EnterScope();
        _refInput.Enqueue(msg);
    }

    /// <inheritdoc/>
    public Queue<Packet> GetAll()
    {
        Swap();
        return _refOutput;
    }

    /// <summary>
    /// Swaps the input and output queues.
    /// </summary>
    private void Swap()
    {
        using var scope = _writeLock.EnterScope();
        (_refOutput, _refInput) = (_refInput, _refOutput);
    }
}
