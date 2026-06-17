using Shared.Logging;
using System.Text;

namespace Shared.EventBus.EventTypes
{
    public abstract record BusEvent
    {
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public EventMessageType Category { get; init; }
        public LogLevel EventSeverity { get; init; }

        protected BusEvent(EventMessageType category, LogLevel eventSeverity)
        {
            Category = category;
            EventSeverity = eventSeverity;
        }

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            return false; // Suppress OccurredAt, Category, EventSeverity from derived ToString()
        }
    }
}
