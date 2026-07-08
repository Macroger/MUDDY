using Client.Core.Infrastructure.Events;
using Client.Core.State.Player;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;

namespace Client.Core.State.Supervisors
{
    public class PlayerStateSupervisor: IDisposable
    {
        private readonly IEventBus _eventBus = null!;
        private PlayerState? _player = null!;
        private readonly List<ISubscriptionToken> _subscriptions = new();
        private bool _disposed = false;

        public PlayerStateSupervisor(IEventBus eventBus) 
        {
            _eventBus  = eventBus;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            // Subscribe to and listen for outbound message events
            _subscriptions.Add(_eventBus.Subscribe<PlayerStateEvents.Notifications.PlayerStateUpdate>(
                eventType: EventMessageType.Player,
                handler: OnPlayerStateUpdate
            ));
        }

        private void OnPlayerStateUpdate(PlayerStateEvents.Notifications.PlayerStateUpdate update)
        {
            // Validate the incomming update
            if(update == null)
            {
                _eventBus.Publish(
                    EventMessageType.Player,
                    new PlayerStateEvents.Errors.PlayerStateError
                    (
                        "Received null player state update.",
                        new ArgumentNullException(nameof(update))
                    )
                );

                return;
            }

            // Update the player state with the new information
            _player = update.updatedPlayerState;

            _eventBus.Publish(
                EventMessageType.Gui,
                new ClientGuiEvents.Notifications.PlayerStateUpdate(_player)
            );
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
