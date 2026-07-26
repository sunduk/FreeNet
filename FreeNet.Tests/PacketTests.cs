using NSubstitute;
using VerifyTests;

namespace FreeNet.Tests;

public class PacketTests
{
    [Test]
    public async Task Packet_round_trip_preserves_payload()
    {
        var packet = CPacket.create(100);
        packet.push((short)7);
        packet.push(42);
        packet.push("hello");
        packet.record_size();

        var parsed = new CPacket(new ArraySegment<byte>(packet.buffer, 0, packet.position), null!);

        await Assert.That(parsed.protocol_id).IsEqualTo((short)100);
        await Assert.That(parsed.pop_protocol_id()).IsEqualTo((short)100);
        await Assert.That(parsed.pop_int16()).IsEqualTo((short)7);
        await Assert.That(parsed.pop_int32()).IsEqualTo(42);
        await Assert.That(parsed.pop_string()).IsEqualTo("hello");
    }

    [Test]
    public void Close_ack_message_notifies_peer()
    {
        var peer = Substitute.For<IPeer>();
        var token = new CUserToken(null!);
        token.set_peer(peer);

        var closeAck = CPacket.create(-1);
        closeAck.record_size();
        var message = new CPacket(new ArraySegment<byte>(closeAck.buffer, 0, closeAck.position), token);

        token.on_message(message);

        peer.Received(1).on_removed();
    }

    [Test]
    public async Task Verify_configuration_is_available()
    {
        VerifierSettings.DontScrubGuids();
        var value = Guid.NewGuid();
        await Assert.That(value).IsNotEqualTo(Guid.Empty);
    }
}
