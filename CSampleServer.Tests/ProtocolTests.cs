using GameServer;
using VerifyTests;

namespace CSampleServer.Tests;

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

    [Test]
    public async Task Verify_configuration_is_available()
    {
        VerifierSettings.DontScrubGuids();
        var value = Guid.NewGuid();
        await Assert.That(value).IsNotEqualTo(Guid.Empty);
    }
}
