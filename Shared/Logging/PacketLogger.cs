using Shared.EventBus;
using Shared.EventBus.SubscriptionToken;
using Shared.Logging;
using System.Collections.Concurrent;

namespace Client.Core.Network
{
    /// <summary>
    /// Subscribes to PacketLog events and stores/logs all packet transmissions.
    /// Optionally writes packet events to a log file.
    /// </summary>
    public class PacketLogger : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ConcurrentBag<EventReason> _packetEvents = new();
        private readonly ISubscriptionToken _subscriptionToken;
        private readonly ILogFileWriter? _fileWriter;
        private readonly object _writeLock = new();

        public PacketLogger(IEventBus eventBus, ILogFileWriter? fileWriter = null)
        {
            _eventBus = eventBus;
            _fileWriter = fileWriter;
            // Subscribe to PacketLog channel
            _subscriptionToken = _eventBus.Subscribe<EventEnvelope>(EventMessageType.PacketLog, OnPacketEvent);
        }

        private void OnPacketEvent(EventEnvelope envelope)
        {
            if (envelope.Payload is EventReason reason)
            {
                _packetEvents.Add(reason);

                // Write to file if a file writer is configured
                if (_fileWriter != null)
                {
                    string timestamp = envelope.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logLine = $"[{timestamp} UTC] {reason.Message}";

                    if (reason.Data != null)
                    {
                        logLine += $" | Data: {reason.Data}";
                    }

                    lock (_writeLock)
                    {
                        _fileWriter.WriteLine(logLine);
                    }
                }
            }
        }

        public EventReason[] GetAllPacketEvents() => _packetEvents.ToArray();

        public void Dispose()
        {
            _subscriptionToken.Dispose();
            _fileWriter?.Dispose();
        }
    }
}
