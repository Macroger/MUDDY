using Shared.EventBus;
using Shared.Protocol.Types;

namespace Shared.Logging
{
    public sealed class EventBusLogger
    {
        private readonly LogLevel _minimumLogLevel;

        public EventBusLogger(IEventBus bus, LogLevel minimumLevel)
        {
            _minimumLogLevel = minimumLevel;
            bus.Subscribe(EventMessageType.Log, HandleLog);
        }

        private void HandleLog(EventEnvelope envelope)
        {
            if (envelope.Payload is not LogRecord log) return;
            if (log.Level < _minimumLogLevel) return;

            Console.WriteLine(
                $"[{log.Level}] " +
                $"[{log.Source}] " +
                $"{log.Message} " +
                $"{(log.ConnectionId is null ? "" : $"Conn={log.ConnectionId}")}"
            );
        }
    }
}
