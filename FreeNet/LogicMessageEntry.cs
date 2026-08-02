using System.Threading.Channels;

namespace FreeNet;

/// <summary>
/// Receives completed packets and dispatches them on the logic thread.
/// </summary>
public class LogicMessageEntry(NetworkService service) : IMessageDispatcher
{
    private readonly CancellationTokenSource _logicCancellation = new();

    private readonly Channel<Packet> _messageChannel = Channel.CreateUnbounded<Packet>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private Task? _logicTask;

    /// <inheritdoc/>
    public void OnMessage(UserToken user, ArraySegment<byte> buffer)
    {
        // Called on the I/O thread. Enqueue the completed packet.
        Packet msg = new(buffer, user);
        _ = _messageChannel.Writer.TryWrite(msg);
    }

    /// <summary>
    /// Starts the logic dispatcher loop.
    /// </summary>
    public void Start()
    {
        if (_logicTask is not null)
        {
            return;
        }

        _logicTask = DoLogicAsync(_logicCancellation.Token);
    }

    /// <summary>
    /// Stops the logic dispatcher loop.
    /// </summary>
    public void Stop()
    {
        _logicCancellation.Cancel();
        _ = _messageChannel.Writer.TryComplete();
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

    private async Task DoLogicAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _messageChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                Queue<Packet> pending = new();
                while (_messageChannel.Reader.TryRead(out var msg))
                {
                    pending.Enqueue(msg);
                }

                DispatchAll(pending);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
