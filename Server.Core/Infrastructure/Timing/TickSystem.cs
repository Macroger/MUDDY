// =============================================================================
/// @file       TickSystem.cs
/// @namespace  Server.Core.Infrastructure.Timing
/// @brief      Produces periodic tick events for server game loop coordination.
/// @details    Runs asynchronously on its own task. Must be disposed to ensure
///             graceful shutdown and completion of the tick loop.
// =============================================================================
using Server.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using System;
using System.Diagnostics;

namespace Server.Core.Infrastructure.Timing
{
    public class TickSystem : ITickSystem, IDisposable
    {
        /// <summary>
        /// A reference to the eventBus to publish tick events to.
        /// </summary>
        private readonly IEventBus _eventBus;

        /// <summary>
        /// The current tick count.
        /// </summary>
        private long _currentTick = 0;

        /// <summary>
        /// Ticks per second.
        /// </summary>
        private readonly int _tickRate;

        /// <summary>
        /// Cancellation token source used to signal the tick loop to stop.
        /// </summary>
        private CancellationTokenSource? _cancellationTokenSource = null;

        /// <summary>
        /// The task running the tick loop, tracked for clean disposal.
        /// </summary>
        private Task? _tickLoopTask = null;

        /// <summary>
        /// Stopwatch used to measure delta time between ticks.
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// Indicates whether the tick system has been disposed.
        /// </summary>
        private bool _disposed = false;

        public TickSystem(IEventBus eventBus, int tickRate = 10)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _tickRate = tickRate;
        }

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TickSystem));
            if (_cancellationTokenSource != null) throw new InvalidOperationException("Tick system is already running.");

            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch.Restart();
            _tickLoopTask = RunTickLoopAsync(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (_cancellationTokenSource == null) return;

            _cancellationTokenSource.Cancel();
        }

        private async Task RunTickLoopAsync(CancellationToken cancellationToken)
        {
            double lastElapsed = 0.0;

            while (!cancellationToken.IsCancellationRequested)
            {
                double currentElapsed = _stopwatch.Elapsed.TotalMilliseconds;
                double deltaTime = currentElapsed - lastElapsed;
                lastElapsed = currentElapsed;

                // Publish a tick event with the current tick count and delta time.
                _eventBus.Publish(
                    EventMessageType.System,
                    new TickEvents.Timing.GameTickEvent(_currentTick, deltaTime)
                );

                // Increment the tick count for the next tick.
                _currentTick++;

                // Wait for the next tick based on the tick rate.
                try
                {
                    await Task.Delay(1000 / _tickRate, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when Stop() is called; exit gracefully.
                    break;
                }
            }

            _stopwatch.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();

            // Wait for the tick loop to complete (with timeout for safety).
            if (_tickLoopTask != null)
            {
                try
                {
                    _tickLoopTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                    // Task was cancelled or faulted; already stopped.
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _tickLoopTask = null;

            _disposed = true;
        }
    }
}