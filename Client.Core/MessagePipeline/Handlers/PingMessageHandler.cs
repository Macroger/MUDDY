using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles ping messages from the server.
    /// </summary>
    public sealed class PingMessageHandler : IMessageHandler
    {
        private readonly IEventBus _eventBus = null!;
        public PacketType MessageType { get; init; } = PacketType.Ping;
        public PingMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {
            // Respond to the ping message by sending a ping back to the server.
            _eventBus.Publish(
                EventMessageType.Network,
                new ClientNetworkEvents.Commands.SendPingToServer()
            );

            return;
        }
    }
}
