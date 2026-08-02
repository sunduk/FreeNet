using FreeNet;

namespace Protocol;

public class CSMoveReq : AProtocolMessage<CSMoveReq>
{
    public CSMoveReq(Packet? msg = null)
        : base(PacketProtocol.MOVE_REQ)
    {
        if (msg is not null)
        {
            _ = FromPacket(msg);
        }
    }

    public float Rotation { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public override CSMoveReq FromPacket(Packet msg)
    {
        X = msg.PopFloat();
        Y = msg.PopFloat();
        Z = msg.PopFloat();
        Rotation = msg.PopFloat();

        return this;
    }

    public override Packet ToPacket()
    {
        var msg = Packet.Create(PacketProtocol.ToShort());

        msg.Push(X);
        msg.Push(Y);
        msg.Push(Z);
        msg.Push(Rotation);

        return msg;
    }

    public override string ToString() => $"{PacketProtocol} Pos ({X}, {Y}, {Z}) rot : {Rotation} deg";
}
