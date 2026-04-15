namespace Shared.EventBus
{
    /// <summary>
    /// Helper class for publishing events to the event bus. This class provides a convenient method for publishing events with a specific message type and reason.
    /// </summary>
    public static class EventBusHelper
    {
        /// <summary>
        /// Publishes an event to the event bus with the specified message type and domain event payload.
        /// </summary>
        /// <typeparam name="T"> The type of the domain event payload. Must be a reference type. </typeparam>
        /// <param name="eventBus"> A reference to the event bus to which the event will be published. </param>
        /// <param name="type"> The category of the event, represented by an EventMessageType enum value. This helps subscribers filter and handle events based on their type. </param>
        /// <param name="eventData"> The actual event data or payload that will be sent to subscribers. This can be any reference type that contains the relevant information about the event. </param>
        public static void PublishEvent<T>(
            IEventBus eventBus,
            EventMessageType type,
            T eventData)
            where T : class
        {
            eventBus.Publish(type, eventData);
        }

        /// <summary>
        /// Publishes an event to the event bus with the specified message type and reason.
        /// </summary>
        /// <param name="eventBus">The event bus to publish to.</param>
        /// <param name="type">The category of the event.</param>
        /// <param name="reason">The reason and data associated with the event.</param>
        public static void PublishEvent(
            IEventBus eventBus,
            EventMessageType type,
            EventReason reason)
        {
            eventBus.Publish(type, new EventEnvelope(type, reason));
        }
    }
}
