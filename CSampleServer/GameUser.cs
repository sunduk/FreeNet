using FreeNet;

namespace CSampleServer;

/// <summary>
/// 하나의 session객체를 나타낸다.
/// </summary>
internal class GameUser : IPeer
{
    private readonly UserToken _token;

    public GameUser(UserToken token)
    {
        _token = token;
        _token.SetPeer(this);
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        _token.Ban();
    }

    /// <inheritdoc/>
    public void OnMessage(Packet msg)
    {
        // 에코서버 테스트할 때 사용함.
        // Remove below comments to use echo server.
        //send(msg);
        //return;

        // ex)
        PROTOCOL protocolId = (PROTOCOL)msg.PopProtocolId();
        //Console.WriteLine("------------------------------------------------------");
        //Console.WriteLine("protocol id " + protocolId);
        switch (protocolId)
        {
            case PROTOCOL.CHAT_MSG_REQ:
                {
                    string text = msg.PopString();
                    Console.WriteLine(string.Format("text {0}", text));

                    Packet response = Packet.Create((short)PROTOCOL.CHAT_MSG_ACK);
                    response.Push(text);
                    Send(response);

                    if (text.Equals("exit"))
                    {
                        // 대량의 메시지를 한꺼번에 보낸 후 종료하는 시나리오 테스트.
                        for (int i = 0; i < 1000; ++i)
                        {
                            Packet dummy = Packet.Create((short)PROTOCOL.CHAT_MSG_ACK);
                            dummy.Push(i.ToString());
                            Send(dummy);
                        }

                        _token.Ban();
                    }
                }

                break;
        }
    }

    /// <inheritdoc/>
    public void OnRemoved()
    {
        //Console.WriteLine("The client disconnected.");

        Program.RemoveUser(this);
    }

    /// <inheritdoc/>
    public void Send(ArraySegment<byte> data)
    {
        _token.Send(data);
    }

    /// <inheritdoc/>
    public void Send(Packet msg)
    {
        msg.RecordSize();
        _token.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
    }
}