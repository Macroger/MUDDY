using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles authentication event messages from the server.
    /// </summary>
    public sealed class SystemMessageHandler : IMessageHandler
    {
        private IEventBus _eventBus;

        public SystemMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {
            // Publish the message to the GUI via the event bus
            _eventBus.Publish(
                EventMessageType.Gui,
                new ClientGuiEvents.Notifications.ReceivedSystemMessage(envelope)
            );

            return;
        }
    }
}
