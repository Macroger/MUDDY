using Client.Core.MessagePipeline.Routers;
using Shared.EventBus;
using Shared.Network.Types;

namespace Client.Core.MessagePipeline.Registrators
{
    public class ResponseMessageRegistrator
    {
        private readonly IEventBus _eventBus;

        public ResponseMessageRegistrator(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Register(IMessageRouter router)
        {
            var handler = new Handlers.ResponseMessageHandler(_eventBus);
            router.RegisterHandler(PacketType.Response, handler);
        }
    }
}
