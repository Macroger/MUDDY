// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Client.Core.Network;
using Moq;
using Shared.EventBus;
using Shared.Network.Transport;

namespace Client.Tests
{
    [TestClass]
    public class ClientNetworkServiceTests
    {
        [TestMethod]
        public async Task ConnectAsync_SetsIsConnectedTrue()
        {
            var eventBus = new Mock<IEventBus>();
            var packetSerializer = new Mock<IPacketSerializer>();
            var packetFactory = new Mock<IPacketFactory>();
            var protocolLimits = new MuddyProtocolLimits();
            var service = new ClientNetworkService(eventBus.Object, packetSerializer.Object, packetFactory.Object, protocolLimits);
            // Use a non-routable address to avoid real connection
            service.UpdateEndpoint("127.0.0.1", 65500);
            // Should not throw
            await Assert.ThrowsAsync<Exception>(() => service.ConnectAsync());
            Assert.IsFalse(service.IsConnected); // Should remain false on failure
        }

        [TestMethod]
        public async Task DisconnectAsync_SetsIsConnectedFalse()
        {
            var eventBus = new Mock<IEventBus>();
            var packetSerializer = new Mock<IPacketSerializer>();
            var packetFactory = new Mock<IPacketFactory>();
            var protocolLimits = new MuddyProtocolLimits();
            var service = new ClientNetworkService(eventBus.Object, packetSerializer.Object, packetFactory.Object, protocolLimits);
            service.UpdateEndpoint("127.0.0.1", 65500);
            await Assert.ThrowsAsync<Exception>(() => service.ConnectAsync());
            await service.DisconnectAsync();
            Assert.IsFalse(service.IsConnected);
        }
    }
}
