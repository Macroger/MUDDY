using Shared.EventBus.SubscriptionToken;
using Shared.Protocol.Types;

namespace Shared.EventBus
{
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a strongly-typed domain event.
        /// </summary>
        /// <typeparam name="T">The type of event being published.</typeparam>
        /// <param name="event">The event to publish.</param>
        void Publish<T>(T @event) where T : class;

        /// <summary>
        /// Subscribes to a strongly-typed domain event by category.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to.</typeparam>
        /// <param name="category">The message category for filtering.</param>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
        ISubscriptionToken Subscribe<T>(EventMessageType category, Action<T> handler) where T : class;

        ///// <summary>
        ///// Publishes a raw event envelope.
        ///// </summary>
        //void Publish(EventEnvelope envelope);

        ///// <summary>
        ///// Subscribes to raw event envelopes by type.
        ///// </summary>
        //ISubscriptionToken Subscribe(EventMessageType messageType, Action<EventEnvelope> handler);

        ///// <summary>
        ///// Subscribes to all raw event envelopes.
        ///// </summary>
        //ISubscriptionToken SubscribeAll(Action<EventEnvelope> handler);
    }
}
