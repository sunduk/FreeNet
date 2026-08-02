namespace CSampleClient.Tests;

public class ProtocolTests
{
    [Test]
    public async Task Chat_protocol_values_are_stable()
    {
        var req = (short)Protocol.PacketProtocol.CHAT_MSG_REQ;
        var ack = (short)Protocol.PacketProtocol.CHAT_MSG_ACK;

        _ = await Assert.That(req).IsEqualTo((short)1);
        _ = await Assert.That(ack).IsEqualTo((short)2);
    }
}
