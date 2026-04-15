using Shared.EventBus;
using Shared.EventBus.SubscriptionToken;
using System.Collections.Concurrent;

namespace Client.Core.Network
{
    /// <summary>
    /// Subscribes to PacketLog events and stores/logs all packet transmissions.
    /// </summary>
    public class PacketLogger
    {
        private readonly IEventBus _eventBus;
        private readonly ConcurrentBag<EventReason> _packetEvents = new();
        private readonly ISubscriptionToken _subscriptionToken;

        public PacketLogger(IEventBus eventBus)
        {
            _eventBus = eventBus;
            // Subscribe to PacketLog channel
            _subscriptionToken = _eventBus.Subscribe<EventEnvelope>(EventMessageType.PacketLog, OnPacketEvent);
        }

        private void OnPacketEvent(EventEnvelope envelope)
        {
            if (envelope.Payload is EventReason reason)
            {
                _packetEvents.Add(reason);
                // You can add additional logic here, e.g., write to file, database, etc.
            }
        }

        public EventReason[] GetAllPacketEvents() => _packetEvents.ToArray();

        public void Dispose()
        {
            _subscriptionToken.Dispose();
        }
    }
}
