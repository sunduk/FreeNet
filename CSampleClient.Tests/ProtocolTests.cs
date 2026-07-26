using GameServer;

namespace CSampleClient.Tests;

public class ProtocolTests
{
    [Test]
    public async Task Chat_protocol_values_are_stable()
    {
        var req = (short)PROTOCOL.CHAT_MSG_REQ;
        var ack = (short)PROTOCOL.CHAT_MSG_ACK;

        await Assert.That(req).IsEqualTo((short)1);
        await Assert.That(ack).IsEqualTo((short)2);
    }
}
