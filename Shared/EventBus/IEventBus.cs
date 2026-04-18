using Shared.EventBus.SubscriptionToken;

namespace Shared.EventBus
{
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a strongly-typed domain event to subscribers.
        /// </summary>
        /// <typeparam name="T">The type of event being published.</typeparam>
        /// <param name="category">The message category for routing.</param>
        /// <param name="newEvent">The event to publish.</param>
        void Publish<T>(EventMessageType eventType, T newEvent) where T : class;

        /// <summary>
        /// Subscribes to a strongly-typed domain event in a specific category.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to.</typeparam>
        /// <param name="category">The message category to filter by.</param>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        ISubscriptionToken Subscribe<T>(EventMessageType eventType, Action<T> handler) where T : class;


        /// <summary>
        /// Subscribes to all raw event envelopes.
        /// </summary>
        ISubscriptionToken SubscribeAll(Action<object> handler);
    }
}
