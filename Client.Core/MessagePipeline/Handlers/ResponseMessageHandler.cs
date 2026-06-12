using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles authentication event messages from the server.
    /// </summary>
    public sealed class ResponseMessageHandler : IMessageHandler
    {
        private IEventBus _eventBus;
        public PacketType MessageType { get; init; }

        public ResponseMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
            MessageType = PacketType.Response;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {
            // Publish the message to the GUI via the event bus
            _eventBus.Publish(
                EventMessageType.Gui,
                new ClientGuiEvents.Notifications.ReceivedResponseMessage(envelope)
            );

            return;
        }
    }
}
