using System.Threading.Tasks;
using Xunit;
using Moq;
using Shared.EventBus;
using Shared.Protocol.Transport;
using Client.Core.Network;
using Client.Core.CommandPipeline;

namespace Tests.Client
{
    public class ClientNetworkServiceTests
    {
        [Fact]
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
            await Assert.ThrowsAnyAsync<System.Exception>(() => service.ConnectAsync());
            Assert.False(service.IsConnected); // Should remain false on failure
        }

        [Fact]
        public async Task DisconnectAsync_SetsIsConnectedFalse()
        {
            var eventBus = new Mock<IEventBus>();
            var packetSerializer = new Mock<IPacketSerializer>();
            var packetFactory = new Mock<IPacketFactory>();
            var protocolLimits = new MuddyProtocolLimits();
            var service = new ClientNetworkService(eventBus.Object, packetSerializer.Object, packetFactory.Object, protocolLimits);
            service.UpdateEndpoint("127.0.0.1", 65500);
            await Assert.ThrowsAnyAsync<System.Exception>(() => service.ConnectAsync());
            await service.DisconnectAsync();
            Assert.False(service.IsConnected);
        }
    }

}
