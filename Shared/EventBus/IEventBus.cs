// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;

namespace Shared.EventBus
{
    public interface IEventBus: IDisposable
    {
        /// <summary>
        /// Publishes a strongly-typed domain event to subscribers.
        /// </summary>
        /// <typeparam name="T">The type of event being published</typeparam>
        /// <param name="category">The message category for routing</param>
        /// <param name="newEvent">The event to publish</param>
        void Publish<T>(EventMessageType eventType, T newEvent) where T : BusEvent;

        /// <summary>
        /// Subscribes to a strongly-typed domain event in a specific category.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to</typeparam>
        /// <param name="category">The message category to filter by</param>
        /// <param name="handler">The handler to invoke when the event is published</param>
        /// <returns>A subscription token that can be disposed to unsubscribe</returns>
        ISubscriptionToken Subscribe<T>(EventMessageType eventType, Action<T> handler) where T : BusEvent;

        /// <summary>
        /// Subscribes to all events of a specific category, regardless of their type.
        /// This allows you to receive any event that falls under the specified category.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        ISubscriptionToken SubscribeToCategory(EventMessageType eventType, Action<object> handler);

        /// <summary>
        /// Subscribes to all events that occur on the event bus, regardless of type or category.
        /// This is useful for logging, monitoring, or debugging purposes where we want to capture every event that is published.
        /// </summary>
        /// <param name="handler">The call-back method. This gets executed when any event occurs on the bus. </param>
        /// <returns></returns>
        ISubscriptionToken SubscribeAll(Action<object> handler);
    }
}
