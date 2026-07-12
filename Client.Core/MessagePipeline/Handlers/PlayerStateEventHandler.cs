using Shared.EventBus;
using Shared.Network.Transport;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Handlers
{
    public class PlayerStateEventHandler : IMessageHandler
    {
        private readonly IEventBus _eventBus = null!;

        public PacketType MessageType { get; init; } = PacketType.PlayerStateEvent;

        public PlayerStateEventHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public Task ExecuteAsync(PacketEnvelope envelope)
        {
            throw new NotImplementedException();
        }
    }
}
