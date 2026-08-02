using FreeNet;
using Protocol;

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

        var protocolId = msg.PopProtocolId().ToProtocol();
        switch (protocolId)
        {
            //case EPacketProtocol.CHAT_MSG_ACK:
            //	{
            //		string text = msg.pop_string();
            //		Console.WriteLine(string.Format("received text {0}", text));
            //	}
            //                break;
            case PacketProtocol.USER_INFO:
                {
                    var id = msg.PopInt16();
                    Console.WriteLine(string.Format("yourid {0}", id));
                }

                break;
            case PacketProtocol.MOVE_CAST:
                {
                    var ret = new SCMoveCast(msg);
                    Console.WriteLine(ret.ToString());
                    //short userid = msg.PopInt16();
                    //float x = msg.PopFloat();
                    //float y = msg.PopFloat();
                    //float z = msg.PopFloat();
                    //float r = msg.PopFloat();
                    //Console.WriteLine($"move {userid} {x} {y} {z} {r}");
                }

                break;
            default:
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
