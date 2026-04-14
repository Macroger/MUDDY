namespace Shared.EventBus
{
    /// <summary>
    /// Helper class for publishing events to the event bus. This class provides a convenient method for publishing events with a specific message type and reason.
    /// </summary>
    public static class EventBusHelper
    {
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
