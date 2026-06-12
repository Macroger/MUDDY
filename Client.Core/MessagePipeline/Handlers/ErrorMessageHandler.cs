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
    public sealed class ErrorMessageHandler : IMessageHandler
    {
        private IEventBus _eventBus;
        public PacketType MessageType { get; init; }
        public ErrorMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
            MessageType = PacketType.Error;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {

            // Publish the message to the GUI via the event bus
            _eventBus.Publish(
                EventMessageType.Gui,
                new ClientGuiEvents.Notifications.ReceivedEventMessage(envelope)
            );

            return;
        }
    }
}
