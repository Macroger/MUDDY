using Server.Core.Network.Packet;
using Shared.Identity;
using Shared.Protocol.Transport;
using Shared.Protocol.Types;

namespace Server.Tests.Network.Packet;

[TestClass]
public class PacketSerializerTests
{
    private MuddyProtocolLimits _limits;
    private MuddyPacketSerializer _serializer;
    private MuddyPacketFactory _packetFactory;
    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        _limits = new MuddyProtocolLimits();
        _serializer = new MuddyPacketSerializer(_limits);
        _packetFactory = new MuddyPacketFactory();
    }


    [TestMethod]
    public void PacketSerializer_SerializesDeserializesCorrectly()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02, 0x03 };        
        
        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(123),
            (ProtocolMessageType)1,
            (MessageFlags)0,
            body
            );

        // Calculate the expected size of the serialized packet
        int expectedSize = _limits.headerSize + _limits.tailSize + body.Length;

        // Create a new MuddyPacket using the factory
        var newPacket = _packetFactory.CreateMuddyPacket(message);

        Assert.AreEqual((uint)body.Length, newPacket.Header.BodyLength);

        // Act
        var packet = _serializer.Serialize(newPacket);

        Assert.IsNotNull(packet);
        Assert.IsNotEmpty(packet);
        Assert.HasCount(expectedSize, packet);

        MuddyPacket deserializedPacket = _serializer.Deserialize(packet);
    }

    [TestMethod]
    public void PacketSerializer_SerializesPacketCorrectly()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02, 0x03 };

        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(123),
            (ProtocolMessageType)1,
            (MessageFlags)0,
            body
            );

        // Calculate the expected size of the serialized packet
        int expectedSize = _limits.headerSize + _limits.tailSize + body.Length;

        // Create a new MuddyPacket using the factory
        var newPacket = _packetFactory.CreateMuddyPacket(message);

        // Act
        var packet = _serializer.Serialize(newPacket);

        Assert.IsNotNull(packet);
        Assert.IsNotEmpty(packet);
        Assert.HasCount(expectedSize, packet);
    }



    [TestMethod]
    public void PacketSerializer_AllowsZeroLengthBody()
    {
        // Arrange
        var body = Array.Empty<byte>();
        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(1),
            ProtocolMessageType.Ping,
            MessageFlags.None,
            body);

        var packet = _packetFactory.CreateMuddyPacket(message);

        // Act
        byte[] serialized = _serializer.Serialize(packet);
        MuddyPacket deserialized = _serializer.Deserialize(serialized);

        // Assert
        Assert.AreEqual(0u, deserialized.Header.BodyLength);
        CollectionAssert.AreEqual(Array.Empty<byte>(), deserialized.Body);

        int expectedSize = _limits.headerSize + _limits.tailSize;

        Assert.HasCount(expectedSize, serialized);
    }

    [TestMethod]
    public void PacketSerializer_ThrowsIfBodyLengthMismatch()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02 };
        var header = new MuddyPacketHeader
        {
            BodyLength = 10, // incorrect on purpose
            MsgId = 1,
            MsgType = (ushort)ProtocolMessageType.Chat,
            BitFlags = 0
        };

        var packet = new MuddyPacket(header, body);

        // Act + Assert
        Assert.Throws<InvalidDataException>(() => _serializer.Serialize(packet));
    }

    [TestMethod]
    public void PacketSerializer_ThrowsOnTruncatedPacket()
    {
        // Arrange
        byte[] truncated = new byte[_limits.headerSize + _limits.tailSize - 1];

        // Act + Assert
        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(truncated));
    }

    [TestMethod]
    public void PacketSerializer_ThrowsOnPacketLengthMismatch()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02 };
        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(1),
            ProtocolMessageType.Chat,
            MessageFlags.None,
            body);

        var packet = _packetFactory.CreateMuddyPacket(message);
        byte[] serialized = _serializer.Serialize(packet);

        // Corrupt the BodyLength field in header
        serialized[0] = 0x10; // BODY length becomes 16 arbitrarily

        // Act + Assert
        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(serialized));
    }

    [TestMethod]
    public void PacketSerializer_ThrowsOnCrcMismatch()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02, 0x03 };
        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(1),
            ProtocolMessageType.Chat,
            MessageFlags.None,
            body);

        var packet = _packetFactory.CreateMuddyPacket(message);
        byte[] serialized = _serializer.Serialize(packet);

        // Corrupt last byte of CRC
        serialized[^1] ^= 0xFF;

        // Act + Assert
        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(serialized));
    }

    [TestMethod]
    public void PacketSerializer_ThrowsOnOversizedBody()
    {
        // Arrange
        byte[] body = new byte[_limits.MaxJsonBodyBytes + 1];

        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(1),
            ProtocolMessageType.Chat,
            MessageFlags.None,
            body);

        var packet = _packetFactory.CreateMuddyPacket(message);
        byte[] serialized = _serializer.Serialize(packet);

        // Act + Assert
        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(serialized));
    }

    [TestMethod]
    public void PacketSerializer_DeserializesHeaderCorrectly()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02 };
        var message = new Shared.Protocol.Types.ProtocolEnvelope(
            new MessageId(42),
            ProtocolMessageType.Chat,
            MessageFlags.None,
            body);

        var packet = _packetFactory.CreateMuddyPacket(message);
        byte[] serialized = _serializer.Serialize(packet);
        byte[] headerBytes = serialized.Take(_limits.headerSize).ToArray();

        // Act
        var header = _serializer.DeserializeHeader(headerBytes);

        // Assert
        Assert.AreEqual((uint)body.Length, header.BodyLength);
        Assert.AreEqual(42u, header.MsgId);
    }

}
