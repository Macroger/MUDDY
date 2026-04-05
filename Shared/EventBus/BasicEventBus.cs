using Shared.EventBus.SubscriptionToken;
using Shared.Protocol.Types;

namespace Shared.EventBus
{
    public sealed class BasicEventBus : IEventBus

    {
        private readonly Dictionary<EventMessageType, List<Action<EventEnvelope>>> _subscribers = new();

        private readonly List<Action<EventEnvelope>> _globalSubscribers = new();

        public void Publish(EventEnvelope envelope)
        {
            // Deliver to global subscribers
            foreach (var subscriber in _globalSubscribers)
            {
                subscriber(envelope);
            }

            // Deliver to subscribers of this message type
            if (_subscribers.TryGetValue(envelope.MsgType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(envelope);
                }
            }
        }

        public ISubscriptionToken Subscribe(EventMessageType messageType, Action<EventEnvelope> handler)
        {
            //Check if there is a bucket for this type of eventMessage in the subscribers dict.
            if (!_subscribers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Action<EventEnvelope>>();
                _subscribers[messageType] = handlers;
            }

            handlers.Add(handler);

            // Generate a new subscription token and return it
            return new BasicSubscriptionToken(() => handlers.Remove(handler));
        }

        public ISubscriptionToken SubscribeAll(Action<EventEnvelope> handler)
        {
            _globalSubscribers.Add(handler);

            return new BasicSubscriptionToken( () => _globalSubscribers.Remove(handler));
        }
    }
}
