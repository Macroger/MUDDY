using Shared.EventBus.SubscriptionToken;

namespace Shared.EventBus
{
    public sealed class BasicEventBus : IEventBus, IDisposable
    {
        /// <summary>
        /// An internal interface representing a subscriber to an event. This abstraction allows us to store subscribers 
        /// of different event types in a common collection and invoke them without needing to know their specific type at runtime.
        /// </summary>  
        private interface IEventSubscriber
        {
            /// Invokes the subscriber with the given event.
            void Invoke(object newEvent);
        }

        private class EventSubscriber<T> : IEventSubscriber where T : class
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

        private bool _disposed = false;

        /// <summary>
        /// A collection holding the subscribers for each event category and type.
        /// </summary>
        private readonly Dictionary<EventMessageType, Dictionary<Type, HashSet<IEventSubscriber>>> _subscribers = new();

        /// <summary>
        /// A collection of subscribers that want to receive all events of a specific category, regardless of their type.
        /// </summary>
        private readonly Dictionary<EventMessageType, HashSet<Action<object>>> _categorySubscribers = new();

        /// <summary>
        /// A collection of subscribers that want to receive all events, regardless of category or type.
        /// </summary>
        private readonly HashSet<Action<object>> _globalSubscribers = new();

        /// <summary>
        /// Publishes a strongly-typed event.
        /// </summary>
        public void Publish<T>(EventMessageType category, T newEvent) where T : EventTypes.BusEvent
        {
            // Ensure the event bus has not been disposed before attempting to publish an event.
            ObjectDisposedException.ThrowIf(_disposed, this);

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

                if (_globalSubscribers.Count > 0)
                {
                    foreach (var observer in _globalSubscribers)
                    {
                        try
                        {
                            observer.Invoke(newEvent);
                        }
                        catch (Exception observerEx)
                        {
                            // Log observer exception but continue processing other observers
                            System.Diagnostics.Debug.WriteLine($"[EventBus] Exception in global observer handler for {eventType.Name}: {observerEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"[EventBus] Stack trace: {observerEx.StackTrace}");
                        }                        
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
        /// <returns>
        /// ISubscriptionToken that can be disposed to unsubscribe the handler from the event bus.
        /// </returns>
        public ISubscriptionToken Subscribe<T>(EventMessageType eventCategory, Action<T> handler) where T : EventTypes.BusEvent
        {
            // Ensure the event bus has not been disposed before attempting to subscribe a handler.
            ObjectDisposedException.ThrowIf(_disposed, this);

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

            // Check the typeBucket, see if the event type exists within - if not, create a HashSet for it.
            if (!typeBucket.ContainsKey(eventType))
            {
                typeBucket[eventType] = new HashSet<IEventSubscriber>();
            }

            // Get the set of handlers for this event type within the category.
            var handlers = typeBucket[eventType];

            // Add the subscriber - the HashSet will ensure no duplicates based on the overridden Equals method in EventSubscriber<T>.
            handlers.Add(subscriber);

            // Return a token that removes the subscription when disposed
            return new BasicSubscriptionToken(() => handlers.Remove(subscriber));

        }

        public ISubscriptionToken SubscribeToCategory(EventMessageType category, Action<object> handler)
        {
            // Ensure the event bus has not been disposed before attempting to subscribe a handler.
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Validate the handler argument
            if (handler == null) throw new ArgumentNullException(nameof(handler), "Unable to subscribe - action handler is null.");

            // Ensure the category bucket exists in the category observers dictionary
            if (!_categorySubscribers.ContainsKey(category))
                _categorySubscribers[category] = new HashSet<Action<object>>();

            // Add the handler to the category observers set
            _categorySubscribers[category].Add(handler);

            // Return a token that removes the subscription when disposed
            return new BasicSubscriptionToken(() => _categorySubscribers[category].Remove(handler));
        }

        public ISubscriptionToken SubscribeAll(Action<object> handler)
        {
            // Ensure the event bus has not been disposed before attempting to subscribe a handler.
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Validate the handler argument
            if (handler == null) throw new ArgumentNullException(nameof(handler), "Unable to subscribe - action handler is null.");

            // Add the handler to the global observers set
            _globalSubscribers.Add(handler);

            // Return a token that removes the subscription when disposed
            return new BasicSubscriptionToken(() => _globalSubscribers.Remove(handler));
        }
        
        public void Dispose()
        {
            // Check if the event bus has already been disposed to prevent redundant cleanup operations.
            if (_disposed) return;

            // Mark the event bus as disposed to prevent further operations and allow for garbage collection.
            _disposed = true;

            // Clearing the specific event subscribers
            foreach (var typeBucket in _subscribers.Values)
            {
                // Clearing collection of handlers for each event type within the category
                foreach (var handlerSet in typeBucket.Values)
                    handlerSet.Clear();

                // Clearing the collection of event types for the category
                typeBucket.Clear();
            }

            // Remove the category buckets themselves
            _subscribers.Clear();

            // Clearing category observers
            foreach (var observerSet in _categorySubscribers.Values)
                observerSet.Clear();
            _categorySubscribers.Clear();

            // Clearing global observers
            _globalSubscribers.Clear();
        }
    }
}
