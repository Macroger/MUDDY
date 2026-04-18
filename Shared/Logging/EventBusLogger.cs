using Shared.EventBus;

namespace Shared.Logging
{
    public sealed class EventBusLogger
    {
        private readonly LogLevel _minimumLogLevel;

        public EventBusLogger(IEventBus bus, LogLevel minimumLevel)
        {
            _minimumLogLevel = minimumLevel;
            bus.SubscribeAll(HandleLog);
        }

        private void HandleLog(object envelope)
        {
            if (envelope is not EventEnvelope evt) return;
            if (evt.Payload is not LogRecord log) return;
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
