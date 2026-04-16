using Shared.EventBus.SubscriptionToken;
using Shared.Protocol.Types;

namespace Shared.EventBus
{
    public sealed class BasicEventBus : IEventBus
    {
        /// <summary>
        /// An internal interface.
        /// </summary>  
        private interface IEventSubscriber
        { 
            /// Invokes the subscriber with the given event.
            void Invoke(object newEvent);
        }

        private class EventSubscriber<T>: IEventSubscriber where T : class
        {
            /// <summary>
            /// A reference to the action that will handle the event.
            /// </summary>
            private readonly Action<T> _handler;

            /// Initializes a new instance of the EventSubscriber class with the specified handler.
            public EventSubscriber(Action<T> handler)
            {
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            /// <summary>
            /// Invokes the event handler if the specified event is of the expected type.
            /// </summary>            
            /// <param name="newEvent">The event object to be processed. If the object is of the expected type, it will be passed to the
            /// handler; otherwise, it will be ignored.</param>
            public void Invoke(object newEvent)
            {
                if (newEvent is T typedEvent)
                {
                    _handler(typedEvent);
                }
            }

            /// <summary>
            /// Determines equality based on the underlying handler reference.
            /// </summary>
            public override bool Equals(object? obj) =>
                obj is EventSubscriber<T> other && _handler == other._handler;

            /// <summary>
            /// Returns a hash code based on the underlying handler reference.
            /// </summary>
            public override int GetHashCode() => _handler.GetHashCode();

            
        }

        /// <summary>
        /// A collection holding the subscribers for each event category and type.
        /// </summary>
        private readonly Dictionary<EventMessageType, Dictionary<Type, HashSet<IEventSubscriber>>> _subscribers = new();


        /// <summary>
        /// Global subscribers - stores handlers for all events regardless of type or category.
        /// Uses HashSet to automatically prevent duplicate registrations.
        /// </summary>
        private readonly HashSet<Action<object>> _globalSubscriptionsList = new();


        /// <summary>
        /// Publishes a strongly-typed event.
        /// </summary>
        public void Publish<T>(EventMessageType category, T newEvent) where T : class
        {
            try
            {
                // Validate the newEvent argument
                if (newEvent == null) throw new ArgumentNullException(nameof(newEvent));

                // Get the type of the event being published
                var eventType = typeof(T);

                // Check if there are any typed subscribers for this category
                if (_subscribers.TryGetValue(category, out var typeBucket))
                {
                    // Check if there are handlers for this specific event type                
                    if (typeBucket.TryGetValue(eventType, out var handlers))
                    {
                        // Iterate through the list of handlers - invoking each with event
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                handler.Invoke(newEvent);
                            }
                            catch (Exception handlerEx)
                            {
                                // Log handler exception but continue processing other handlers
                                System.Diagnostics.Debug.WriteLine($"[EventBus] Exception in typed subscriber handler for {eventType.Name}: {handlerEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"[EventBus] Stack trace: {handlerEx.StackTrace}");
                            }
                        }
                    }
                }

                // Also invoke global typed subscribers (subscribe to ALL of type T)
                foreach (var handler in _globalSubscriptionsList)
                {
                    try
                    {
                        handler(newEvent);
                    }
                    catch (Exception globalEx)
                    {
                        // Log global handler exception but continue processing other handlers
                        System.Diagnostics.Debug.WriteLine($"[EventBus] Exception in global subscriber handler for {eventType.Name}: {globalEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[EventBus] Stack trace: {globalEx.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log top-level exception
                System.Diagnostics.Debug.WriteLine($"[EventBus] CRITICAL exception in Publish<{typeof(T).Name}>: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[EventBus] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to signal failure
            }
        }

        /// <summary>
        /// Subscribes a handler to a specific event type within a category.
        /// </summary>
        public ISubscriptionToken Subscribe<T>(EventMessageType eventCategory, Action<T> handler) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler), $"Unable to subscribe - action handler is null. Category: {eventCategory} ");

            // Get the type of the event being published.
            var eventType = typeof(T);

            // Create a new subscriber instance for the provided handler.
            var subscriber = new EventSubscriber<T>(handler);

            // Ensure the category bucket exists in the subscribers dictionary.
            if (!_subscribers.ContainsKey(eventCategory))
            {
                _subscribers[eventCategory] = new Dictionary<Type, HashSet<IEventSubscriber>>();
            }

            // Get the type bucket for the specified category.
            var typeBucket = _subscribers[eventCategory];

            // Ensure the type list exists
            if (!typeBucket.ContainsKey(eventType))
            {
                typeBucket[eventType] = new HashSet<IEventSubscriber>();
            }

            // Get the set of handlers for this event type within the category.
            var handlers = typeBucket[eventType];

            // Add the subscriber if not already present
            handlers.Add(subscriber);

            // Return a token that removes the subscription when disposed
            return new BasicSubscriptionToken(() => handlers.Remove(subscriber));

        }

        /// <summary>
        /// Subscribes a handler to all events, regardless of type or category.
        /// Useful for cross-cutting concerns like logging or monitoring.
        /// </summary>
        public ISubscriptionToken SubscribeAll(Action<object> handler)
        {
            // Validate the handler argument
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // HashSet automatically rejects duplicate handlers
            _globalSubscriptionsList.Add(handler);

            return new BasicSubscriptionToken(() => _globalSubscriptionsList.Remove(handler));
        }
    }
}
