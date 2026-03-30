using Server.Tests;
using Shared.Protocol.Transport;
using Server.Network.Packet;

namespace Server.Tests;

[TestClass]
public class PacketSerializerTests
{
    private MuddyProtocolLimits _limits;
    private MuddyPacketSerializer _serializer

    [TestInitialize]
        public void TestInitialize()
    {
        _limits = new MuddyProtocolLimits();
        _serializer = new MuddyPacketSerializer(_limits);
    }


    [TestMethod]
    public void TestMethod1()
    {
    }
}
