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
    public sealed class AuthenticationMessageHandler : IMessageHandler
    {
        private readonly IEventBus _eventBus = null!;
        public PacketType MessageType { get; init; } = PacketType.Authentication;

        public AuthenticationMessageHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task ExecuteAsync(MessageEnvelope envelope)
        {
            try
            {
                // Check if the envelope contains a session token
                if (envelope.SessionToken is { } sessionToken)
                {
                    // Publish a SessionEstablished event to notify other parts of the system that authentication was successful
                    _eventBus.Publish(
                        EventMessageType.Authentication,
                        new AuthenticationEvents.Notifications.SessionEstablished(sessionToken));
                }
            }
            catch (Exception ex)
            {
                // Log the error via eventBus
                _eventBus.Publish(EventMessageType.Authentication,
                    new AuthenticationEvents.Errors.AuthenticationError("Failed to process authentication message.",ex)
                );
            }

            return;
        }
    }
}
