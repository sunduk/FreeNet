using FreeNet;

namespace CSampleClient;

internal class RemoteServerPeer : IPeer
{
    private int _receivedCount = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteServerPeer"/> class.
    /// </summary>
    /// <param name="token">The token.</param>
    public RemoteServerPeer(UserToken token)
    {
        Token = token;
        Token.SetPeer(this);
    }

    /// <summary>
    /// Gets the token.
    /// </summary>
    /// <value>The token.</value>
    public UserToken Token { get; private set; }

    /// <inheritdoc/>
    public void Disconnect() => Token.Disconnect();

    /// <inheritdoc/>
    public void OnMessage(Packet msg)
    {
        _ = Interlocked.Increment(ref _receivedCount);

        var protocolId = (PROTOCOL)msg.PopProtocolId();
        switch (protocolId)
        {
            case PROTOCOL.CHAT_MSG_ACK:
                var text = msg.PopString();
                Console.WriteLine($"text {text}");
                break;
        }
    }

    /// <inheritdoc/>
    public void OnRemoved()
    {
        Console.WriteLine("Server removed.");
        Console.WriteLine($"recv count {_receivedCount}");
    }

    /// <inheritdoc/>
    public void Send(Packet message)
    {
        message.RecordSize();
        Token.Send(new ArraySegment<byte>(message.Buffer, 0, message.Position));
    }
}
