using Client.Core.MessagePipeline.Routers;
using Shared.EventBus;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Registrators
{
    public class ChatCommandRegistrator : IMessageRegistrator
    {
        private readonly IEventBus _eventBus;

        public ChatCommandRegistrator(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Register(IMessageRouter router)
        {
            var handler = new Handlers.ChatMessageHandler(_eventBus);
            router.RegisterHandler(PacketType.Chat, handler);
        }
    }
}
