using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles player chat event messages from the server.
    /// </summary>
    public sealed class ChatMessageHandler : IMessageHandler
    {
        private IEventBus _eventBus;

        public ChatMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {

            // Publish the message to the GUI via the event bus
            _eventBus.Publish(EventMessageType.Gui,
                new Infrastructure.Events.ClientGuiEvents.Notifications.ReceivedChatMessage(envelope)
            );

            return;
        }
    }
}
