using Client.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using Shared.Identity;

namespace Client.Core.Services.Authentication
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private SessionId _sessionId;
        private ISubscriptionToken _authSubscription = null!;

        public SessionId SessionId => _sessionId;
        public bool IsAuthenticated => _sessionId != SessionId.Unauthenticated;

        public AuthenticationService(IEventBus eventBus)
        {
            _sessionId = SessionId.Unauthenticated;
            
            // Subscribe to auth success events
            _authSubscription = eventBus.Subscribe<AuthenticationEvents.Notifications.SessionEstablished>(
                EventMessageType.Authentication,
                OnSessionEstablished);
        }

        private void OnSessionEstablished(AuthenticationEvents.Notifications.SessionEstablished evnt)
        {
            _sessionId = evnt.id;
        }

        public void Dispose()
        {
            _authSubscription?.Dispose();
        }
    }
}
