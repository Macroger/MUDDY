using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    /// <summary>
    /// Handles ErrorMessages events from the server.
    /// </summary>
    public sealed class ErrorMessageHandler : IMessageHandler
    {
        private readonly IEventBus _eventBus = null!;
        public PacketType MessageType { get; init; } = PacketType.Error;
        public ErrorMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(PacketEnvelope envelope)
        {

            // Publish the message to the GUI via the event bus
            _eventBus.Publish(
                EventMessageType.Gui,
                new ClientGuiEvents.Notifications.ReceivedErrorMessage(envelope)
            );

            return;
        }
    }
}
