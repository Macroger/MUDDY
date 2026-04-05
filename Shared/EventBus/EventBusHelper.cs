using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus
{
    /// <summary>
    /// Helper class for publishing events to the event bus. This class provides a convenient method for publishing events with a specific message type and reason.
    /// </summary>
    public static class EventBusHelper
    {
        public static void PublishEvent(
            IEventBus eventBus,
            EventMessageType type,
            EventReason reason)
        {
            eventBus.Publish(new EventEnvelope(type, reason));
        }
    }
}
