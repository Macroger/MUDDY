using Shared.EventBus.EventTypes;
using Shared.Identity;
using Shared.Logging;

namespace Client.Core.Infrastructure.Events
{
    public class AuthenticationEvents
    {
        public sealed record SessionEstablished(SessionId id) : BusEvent(EventMessageType.Authentication, LogLevel.Debug);
    }
}
