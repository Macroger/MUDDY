// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System.Threading.Channels;

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
        private readonly Channel<string> _logQueue;
        private readonly Task _consumerTask;
        private bool _disposed = false;
        private readonly string _filePath;
        private bool _loggedFailure = false;

        public FileLogger(IEventBus bus, LogLevel minimumLevel, string filePath, int queueCapacity = 10000)
        {
            _minimumLogLevel = minimumLevel;
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            // Create bounded channel with Wait behavior (default)
            // This means producers block when the queue is full
            _logQueue = Channel.CreateBounded<string>(
                new BoundedChannelOptions(queueCapacity)
                {
                    SingleWriter = false,    // Multiple event handlers can write concurrently
                    SingleReader = true,     // Only one background task reads
                    AllowSynchronousContinuations = false,  // Don't run continuations on writer threads
                    FullMode = BoundedChannelFullMode.Wait  // Block producers when full
                });

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

            // Start the background consumer task BEFORE subscribing to the bus
            _consumerTask = ConsumeLogQueueAsync();

            // Subscribe to all bus events (producer)
            _subscription = bus.SubscribeAll(HandleLog);
        }

        /// <summary>
        /// Producer: called by the event bus on the bus thread.
        /// Filters and formats events, then writes them to the channel.
        /// </summary>
        /// <param name="evt">The event object from the bus.</param>
        private void HandleLog(object evt)
        {
            // Filter: only process BusEvent instances
            if (evt is not EventBus.EventTypes.BusEvent busEvent) return;

            // Filter: check severity threshold
            if (busEvent.EventSeverity < _minimumLogLevel) return;

            // Format the log line
            string line = FormatLine(busEvent);

            // Write to the channel (non-blocking if space available, blocks if full)
            // TryWrite returns false immediately if channel is closed
            if (!_logQueue.Writer.TryWrite(line))
            {
                // Channel is completed/closed, log is shutting down
                if (!_loggedFailure)
                {
                    Console.WriteLine($"WARNING: Log queue for '{_filePath}' is closed, dropping log entry");
                    _loggedFailure = true;
                }
            }
        }

        /// <summary>
        /// Consumer: background task that reads from the channel and writes to disk.
        /// Runs until the channel is completed and drained.
        /// </summary>
        /// <returns>A task representing the consumer loop.</returns>
        private async Task ConsumeLogQueueAsync()
        {
            try
            {
                // ReadAllAsync returns an IAsyncEnumerable that completes when:
                // 1. Writer.Complete() is called AND
                // 2. All buffered items have been read
                await foreach (string line in _logQueue.Reader.ReadAllAsync())
                {
                    if (_writer == null)
                    {
                        if (!_loggedFailure)
                        {
                            Console.WriteLine($"WARNING: Cannot write to log file '{_filePath}' - writer not initialized");
                            _loggedFailure = true;
                        }
                        continue;
                    }

                    try
                    {
                        await _writer.WriteLineAsync(line);
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
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: Log consumer task for '{_filePath}' failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"FATAL: Log consumer task for '{_filePath}' failed: {ex}");
            }
        }

        /// <summary>
        /// Formats a <see cref="BusEvent"/> into a single log line string.
        /// </summary>
        /// <param name="busEvent">The event to format.</param>
        /// <returns>A formatted log line string.</returns>
        private static string FormatLine(BusEvent busEvent)
        {
            return $"[{busEvent.OccurredAt:yyyy-MM-dd HH:mm:ss.fff} UTC] " +
                   $"[{busEvent.Category,-12}] " +
                   $"[{busEvent.EventSeverity,-11}] " +
                   $"{busEvent.GetType().Name}: {busEvent}";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Step 1: Unsubscribe from the bus (stop new log entries)
            _subscription.Dispose();

            // Step 2: Signal the channel that no more items will be written
            _logQueue.Writer.Complete();

            // Step 3: Wait for the consumer to drain all buffered entries
            try
            {
                _consumerTask.GetAwaiter().GetResult(); // Blocking wait
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Error waiting for log consumer task to complete: {ex.Message}");
            }

            // Step 4: Dispose the file writer
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
