using Shared.EventBus;
using Shared.EventBus.SubscriptionToken;

namespace Shared.Logging
{
    /// <summary>
    /// Subscribes to all event-bus messages and writes <see cref="LogRecord"/> entries
    /// at or above the configured minimum level to a text file.
    /// The file is opened in append mode, so restarts accumulate into a single log.
    /// </summary>
    public sealed class FileLogger : IDisposable
    {
        private readonly LogLevel _minimumLogLevel;
        private readonly StreamWriter _writer;
        private readonly ISubscriptionToken _subscription;
        private readonly object _writeLock = new();
        private bool _disposed;

        public FileLogger(IEventBus bus, LogLevel minimumLevel, string filePath)
        {
            _minimumLogLevel = minimumLevel;
            _writer = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };
            _subscription = bus.SubscribeAll(HandleLog);
        }

        private void HandleLog(object envelope)
        {
            if (envelope is not EventEnvelope evt) return;
            if (evt.Payload is not LogRecord log) return;
            if (log.Level < _minimumLogLevel) return;

            string line =
                $"[{evt.CreatedAt:yyyy-MM-dd HH:mm:ss.fff} UTC] " +
                $"[{log.Level}] " +
                $"[{log.Source}] " +
                $"{log.Message}" +
                $"{(log.ConnectionId is null ? "" : $" Conn={log.ConnectionId}")}";

            lock (_writeLock)
            {
                if (_disposed) return;
                _writer.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                if (_disposed) return;
                _disposed = true;
                _subscription.Dispose();
                _writer.Dispose();
            }
        }
    }
}
