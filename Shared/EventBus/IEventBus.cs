using Shared.EventBus.SubscriptionToken;
using Shared.Protocol.Types;

namespace Shared.EventBus
{
    public interface IEventBus
    {
        void Publish(EventEnvelope envelope);

        ISubscriptionToken Subscribe(EventMessageType messageType, Action<EventEnvelope> handler);

        ISubscriptionToken SubscribeAll(Action<EventEnvelope> handler);
    }
}
