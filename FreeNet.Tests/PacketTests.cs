using NSubstitute;
using VerifyTests;

namespace FreeNet.Tests;

public class PacketTests
{
    [Test]
    public async Task Packet_round_trip_preserves_payload()
    {
        var packet = Packet.Create(100);
        packet.Push((short)7);
        packet.Push(42);
        packet.Push("hello");
        packet.RecordSize();

        var parsed = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), null!);

        await Assert.That(parsed.ProtocolId).IsEqualTo((short)100);
        await Assert.That(parsed.PopProtocolId()).IsEqualTo((short)100);
        await Assert.That(parsed.PopInt16()).IsEqualTo((short)7);
        await Assert.That(parsed.PopInt32()).IsEqualTo(42);
        await Assert.That(parsed.PopString()).IsEqualTo("hello");
    }

    [Test]
    public void Close_ack_message_notifies_peer()
    {
        var peer = Substitute.For<IPeer>();
        var token = new UserToken(null!);
        token.SetPeer(peer);

        var closeAck = Packet.Create(-1);
        closeAck.RecordSize();
        var message = new Packet(new ArraySegment<byte>(closeAck.Buffer, 0, closeAck.Position), token);

        token.OnMessage(message);

        peer.Received(1).OnRemoved();
    }

    [Test]
    public async Task Verify_configuration_is_available()
    {
        VerifierSettings.DontScrubGuids();
        var value = Guid.NewGuid();
        await Assert.That(value).IsNotEqualTo(Guid.Empty);
    }
}
