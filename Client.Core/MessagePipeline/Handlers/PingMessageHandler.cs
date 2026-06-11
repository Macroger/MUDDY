using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles authentication event messages from the server.
    /// </summary>
    public sealed class PingMessageHandler : IMessageHandler
    {
        private IEventBus _eventBus;

        public PingMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {
            // Responsd to the ping message by sending a ping back to the server.
            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Commands.SendPingToServer()
            );

            return;
        }
    }
}
