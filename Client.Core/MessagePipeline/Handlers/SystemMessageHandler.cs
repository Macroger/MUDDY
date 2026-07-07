using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles system event messages from the server.
    /// </summary>
    public sealed class SystemMessageHandler : IMessageHandler
    {
        private readonly IEventBus _eventBus = null!;
        public PacketType MessageType { get; init; } = PacketType.System;

        public SystemMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(MessageEnvelope envelope)
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
