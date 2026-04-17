using Shared.EventBus;
using Shared.EventBus.SubscriptionToken;
using System;
using System.IO;

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
        private readonly StreamWriter? _writer;
        private readonly ISubscriptionToken _subscription;
        private readonly object _writeLock = new();
        private bool _disposed;
        private readonly string _filePath;
        private bool _loggedFailure = false;

        public FileLogger(IEventBus bus, LogLevel minimumLevel, string filePath)
        {
            _minimumLogLevel = minimumLevel;
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _writer = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // Write startup message
                _writer.WriteLine($"=== FileLogger started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to create log file at '{filePath}': {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to create log file at '{filePath}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                _loggedFailure = true;
            }

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

                if (_writer == null)
                {
                    if (!_loggedFailure)
                    {
                        Console.WriteLine($"WARNING: Cannot write to log file '{_filePath}' - writer not initialized");
                        _loggedFailure = true;
                    }
                    return;
                }

                try
                {
                    _writer.WriteLine(line);
                }
                catch (Exception ex)
                {
                    if (!_loggedFailure)
                    {
                        Console.WriteLine($"ERROR: Failed to write to log file '{_filePath}': {ex.Message}");
                        _loggedFailure = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                if (_disposed) return;
                _disposed = true;
                _subscription.Dispose();

                try
                {
                    _writer?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WARNING: Error disposing log file writer for '{_filePath}': {ex.Message}");
                }
            }
        }
    }
}
