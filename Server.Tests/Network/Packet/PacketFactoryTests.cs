// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.Identity;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Server.Tests.Network.Packet;

[TestClass]
public class PacketFactoryTests
{
    private MuddyPacketFactory _packetFactory = null!;
    private ConnectionId _connectionId;

    [TestInitialize]
    public void TestInitialize()
    {
        _packetFactory = new MuddyPacketFactory();
        _connectionId = new ConnectionId();
    }

    [TestMethod]
    public void Test_PacketFactory_CreatesMuddyPacketCorrectly()
    {
        // Arrange
        var body = new byte[] { 0x01, 0x02, 0x03 };

        var message = new PacketEnvelope(
             connectionId: _connectionId,
             messageId: new MessageId(123),
             messageType: PacketType.Error,
             flags: MessageFlags.None,
             payload: body,
             sessionId: SessionId.Unauthenticated
         );

        // Act
        var newPacket = _packetFactory.CreateMuddyPacket(message);

        // Assert
        Assert.AreEqual((uint)body.Length, newPacket.Header.BodyLength);
        Assert.AreEqual(message.MessageId.Value, newPacket.Header.MsgId);
        Assert.AreEqual(message.MessageType, (PacketType)newPacket.Header.MsgType);
        Assert.AreEqual(message.Flags, (MessageFlags)newPacket.Header.BitFlags);
        Assert.AreEqual(body, newPacket.Body);
    }

    [TestMethod]
    public void Test_PacketFactory_ThrowsWhenMessageNull()
    {
        // Arrange
        PacketEnvelope? message = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => _packetFactory.CreateMuddyPacket(message!));
    }
}
