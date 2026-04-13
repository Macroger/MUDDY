using Shared.EventBus.SubscriptionToken;
using Shared.Protocol.Types;

namespace Shared.EventBus
{
    public sealed class BasicEventBus : IEventBus
    {     
        private readonly Dictionary<EventMessageType, Dictionary<Type, List<object>>> _Subscribers = new();
        private readonly Dictionary<EventMessageType, List<object>> _globalSubscribers = new(); 


        /// <summary>
        /// Publishes a strongly-typed event.
        /// </summary>
        public void Publish<T>(EventMessageType category, T newEvent) where T : class
        {
            if (newEvent == null) throw new ArgumentNullException(nameof(newEvent));

            var eventType = typeof(T);

            // Check if there are any typed subscribers for this category
            if (_Subscribers.TryGetValue(category, out var typeBucket))
            {
                // Check if there are handlers for this specific event type
                
                if (typeBucket.TryGetValue(eventType, out var handlers))
                {
                    // Cast handlers back to Action<T> and invoke each one
                    foreach (var handler in handlers.Cast<Action<T>>())
                    {
                        handler(newEvent);
                    }
                }
            }

            // Also invoke global typed subscribers (subscribe to ALL of type T)
            if (_globalSubscribers.TryGetValue(eventType, out var globalHandlers))
            {
                foreach (var handler in globalHandlers.Cast<Action<T>>())
                {
                    handler(@event);
                }
            }
        }

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

        //public ISubscriptionToken Subscribe(EventMessageType messageType, Action<EventEnvelope> handler)
        //{
        //    //Check if there is a bucket for this type of eventMessage in the subscribers dict.
        //    if (!_subscribers.TryGetValue(messageType, out var handlers))
        //    {
        //        handlers = new List<Action<EventEnvelope>>();
        //        _subscribers[messageType] = handlers;
        //    }

        //    handlers.Add(handler);

        //    // Generate a new subscription token and return it
        //    return new BasicSubscriptionToken(() => handlers.Remove(handler));
        //}

        public ISubscriptionToken Subscribe<T>(EventMessageType category, Action<T> handler) where T : class
        {
            throw new NotImplementedException();
        }

        public ISubscriptionToken SubscribeAll(Action<EventEnvelope> handler)
        {
            _globalSubscribers.Add(handler);

            return new BasicSubscriptionToken( () => _globalSubscribers.Remove(handler));
        }
    }
}
