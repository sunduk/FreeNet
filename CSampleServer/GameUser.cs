using FreeNet;
using Protocol;

namespace CSampleServer;

/// <summary>
/// Represents a single session object.
/// </summary>
internal class GameUser : IPeer
{
    private readonly UserToken _token;

    public GameUser(UserToken token, short sig)
    {
        _token = token;
        Sig = sig;
        _token.SetPeer(this);
    }

    public short Sig { get; private set; }

    /// <inheritdoc/>
    public void Disconnect() => _token.Ban();

    /// <inheritdoc/>
    public void OnMessage(Packet msg)
    {
        var protocol = msg.PopProtocolId().ToProtocol();
        Console.WriteLine("------------------------------------------------------ protocol id " + protocol);

        switch (protocol)
        {
            //case PacketProtocol.CHAT_MSG_REQ:
            //    ProcChat(msg);
            //    break;
            case PacketProtocol.MOVE_REQ:
                ProcMove(msg);
                break;

            default:
                break;
        }
    }

    /// <inheritdoc/>
    public void OnRemoved() =>
        //Console.WriteLine("The client disconnected.");
        Program.RemoveUser(this);

    /// <inheritdoc/>
    public void Send(ArraySegment<byte> data) => _token.Send(data);

    /// <inheritdoc/>
    public void Send(Packet msg)
    {
        msg.RecordSize();
        _token.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
    }

    private void ProcMove(Packet msg)
    {
        var moveReq = new CSMoveReq(msg);
        var ret = new SCMoveCast
        {
            UserID = Sig,
            X = moveReq.X,
            Y = moveReq.Y,
            Z = moveReq.Z,
            Rotation = moveReq.Rotation
        };

        Console.WriteLine(ret.ToString());

        Program.SendAll(ret.ToPacket());

        //short userid = Sig;
        //float x = msg.PopFloat();
        //float y = msg.PopFloat();
        //float z = msg.PopFloat();
        //float r = msg.PopFloat();
        //Console.WriteLine($"move {Sig} {x} {y} {z} {r}");
        //CPacket response = CPacket.create((short)EPacketProtocol.MOVE_CAST);
        //response.push(userid);
        //response.push(x);
        //response.push(y);
        //response.push(z);
        //response.push(r);
        //Program.SendAll(response);
    }

    //private static void ProcChat(CPacket msg)
    //{
    //    string text = msg.pop_string();
    //    Console.WriteLine(string.Format("text {0}", text));

    // CPacket response = CPacket.create((short)EPacketProtocol.CHAT_MSG_ACK); response.push(text); //send(response);

    // Program.SendAll(response);

    //    //if (text.Equals("exit"))
    //    //{
    //    //    for (int i = 0; i < 1000; ++i)
    //    //    {
    //    //        CPacket dummy = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
    //    //        dummy.push(i.ToString());
    //    //        send(dummy);
    //    //    }
    //    //    this.token.ban();
    //    //}
    //}
}
