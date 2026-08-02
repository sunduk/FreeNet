using System;
using System.Collections.Generic;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Receives completed packets and dispatches them on the logic thread.
/// </summary>
public class LogicMessageEntry(NetworkService service) : IMessageDispatcher
{
    private readonly AutoResetEvent _logicEvent = new(false);
    private readonly DoubleBufferingQueue _messageQueue = new();

    /// <inheritdoc/>
    public void OnMessage(UserToken user, ArraySegment<byte> buffer)
    {
        // Called on the I/O thread. Enqueue the completed packet.
        Packet msg = new(buffer, user);
        _messageQueue.Enqueue(msg);

        // Wake the logic thread to process work.
        _ = _logicEvent.Set();
    }

    /// <summary>
    /// Starts the logic thread.
    /// </summary>
    public void Start()
    {
        Thread logic = new(DoLogic)
        {
            IsBackground = true
        };
        logic.Start();
    }

    private void DispatchAll(Queue<Packet> queue)
    {
        while (queue.Count > 0)
        {
            var msg = queue.Dequeue();
            if (msg.Owner is null || !service.Usermanager.Exists(msg.Owner))
            {
                continue;
            }

            msg.Owner.OnMessage(msg);
        }
    }

    /// <summary>
    /// Logic thread loop.
    /// </summary>
    private void DoLogic()
    {
        while (true)
        {
            // A packet arrival will wake this thread.
            _ = _logicEvent.WaitOne();

            // Dispatch messages.
            DispatchAll(_messageQueue.GetAll());
        }
    }
}
