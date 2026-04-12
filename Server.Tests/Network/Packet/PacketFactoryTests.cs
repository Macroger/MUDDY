using Server.Core.Network.Packet;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;
using System.Net.Sockets;

namespace Server.Tests.Network.Packet;

[TestClass]
public class PacketFactoryTests
{
    private MuddyPacketFactory _packetFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        _packetFactory = new MuddyPacketFactory();
    }

    [TestMethod]
    public void Test_PacketFactory_CreatesMuddyPacketCorrectly()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02, 0x03 };

        var message = new Shared.Protocol.Types.TransportEnvelope(
            new MessageId(123),
            (TransportMessageType)1,
            (MessageFlags)0,
            body
            );

        // Act
        var newPacket = _packetFactory.CreateMuddyPacket(message);

        // Assert
        Assert.AreEqual((uint)body.Length, newPacket.Header.BodyLength);
        Assert.AreEqual(message.MessageId.Value, newPacket.Header.MsgId);
        Assert.AreEqual(message.MessageType, (TransportMessageType)newPacket.Header.MsgType);
        Assert.AreEqual(message.Flags, (MessageFlags)newPacket.Header.BitFlags);
        Assert.AreEqual(body, newPacket.Body);
    }

    [TestMethod]
    public void Test_PacketFactory_ThrowsWhenMessageNull()
    {
        // Arrange
        TransportEnvelope? message = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => _packetFactory.CreateMuddyPacket(message!));
    }
}
