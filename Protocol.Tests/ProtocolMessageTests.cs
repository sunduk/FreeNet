using FreeNet;

namespace Protocol.Tests;

public class ProtocolMessageTests
{
    [Test]
    public async Task Packet_protocol_values_are_stable()
    {
        var begin = (short)PacketProtocol.BEGIN;
        var chatReq = (short)PacketProtocol.CHAT_MSG_REQ;
        var chatAck = (short)PacketProtocol.CHAT_MSG_ACK;
        var moveReq = (short)PacketProtocol.MOVE_REQ;
        var moveCast = (short)PacketProtocol.MOVE_CAST;
        var userInfo = (short)PacketProtocol.USER_INFO;
        var end = (short)PacketProtocol.END;

        _ = await Assert.That(begin).IsEqualTo((short)0);
        _ = await Assert.That(chatReq).IsEqualTo((short)1);
        _ = await Assert.That(chatAck).IsEqualTo((short)2);
        _ = await Assert.That(moveReq).IsEqualTo((short)3);
        _ = await Assert.That(moveCast).IsEqualTo((short)4);
        _ = await Assert.That(userInfo).IsEqualTo((short)5);
        _ = await Assert.That(end).IsEqualTo((short)6);
    }

    [Test]
    public async Task Packet_protocol_extension_round_trip_preserves_value()
    {
        const short rawProtocol = 4;
        var typedProtocol = rawProtocol.ToProtocol();

        _ = await Assert.That(typedProtocol).IsEqualTo(PacketProtocol.MOVE_CAST);
        _ = await Assert.That(typedProtocol.ToShort()).IsEqualTo(rawProtocol);
    }

    [Test]
    public async Task CSMoveReq_round_trip_preserves_payload()
    {
        var outbound = new CSMoveReq
        {
            X = 1.25f,
            Y = -2.5f,
            Z = 10.75f,
            Rotation = 270.0f,
        };

        var inbound = new CSMoveReq(ToInboundPacket(outbound.ToPacket()));

        _ = await Assert.That(inbound.X).IsEqualTo(outbound.X);
        _ = await Assert.That(inbound.Y).IsEqualTo(outbound.Y);
        _ = await Assert.That(inbound.Z).IsEqualTo(outbound.Z);
        _ = await Assert.That(inbound.Rotation).IsEqualTo(outbound.Rotation);
    }

    [Test]
    public async Task SCMoveCast_round_trip_preserves_payload()
    {
        var outbound = new SCMoveCast
        {
            UserID = 27,
            X = 0.5f,
            Y = 1.5f,
            Z = 2.5f,
            Rotation = 45.0f,
        };

        var inbound = new SCMoveCast(ToInboundPacket(outbound.ToPacket()));

        _ = await Assert.That(inbound.UserID).IsEqualTo(outbound.UserID);
        _ = await Assert.That(inbound.X).IsEqualTo(outbound.X);
        _ = await Assert.That(inbound.Y).IsEqualTo(outbound.Y);
        _ = await Assert.That(inbound.Z).IsEqualTo(outbound.Z);
        _ = await Assert.That(inbound.Rotation).IsEqualTo(outbound.Rotation);
    }

    [Test]
    public async Task SCUserInfo_round_trip_preserves_payload()
    {
        var outbound = new SCUserInfo
        {
            UserID = 123,
        };

        var inbound = new SCUserInfo(ToInboundPacket(outbound.ToPacket()));

        _ = await Assert.That(inbound.UserID).IsEqualTo(outbound.UserID);
    }

    private static Packet ToInboundPacket(Packet outbound)
    {
        outbound.RecordSize();
        var inbound = new Packet(new ArraySegment<byte>(outbound.Buffer, 0, outbound.Position), null!);

        // Production flow consumes protocol id before message-specific parsing.
        _ = inbound.PopProtocolId();

        return inbound;
    }
}
