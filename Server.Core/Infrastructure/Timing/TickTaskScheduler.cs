// =============================================================================
/// @file       TickTaskScheduler.cs
/// @namespace  Server.Core.Infrastructure.Timing
/// @brief      Tick-based task scheduler implementation with budget enforcement.
/// @details    Subscribes to GameTickEvent and executes scheduled tasks when
///             their target tick arrives. Enforces per-tick task budget to
///             prevent tick overrun. Publishes diagnostic events when budget
///             is exceeded or processing slows down.
// =============================================================================
using Server.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;
using Shared.EventBus.SubscriptionToken;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server.Core.Infrastructure.Timing
{
    public class TickTaskScheduler : ITickTaskScheduler
    {
        private readonly IEventBus _eventBus;
        private readonly SortedDictionary<long, List<ScheduledTask>> _taskQueue = new();
        private readonly ISubscriptionToken _tickSubscription;
        private long _currentTick = 0;
        private bool _disposed = false;

        /// <summary>
        /// Maximum number of tasks to execute per tick before deferring to next tick.
        /// </summary>
        private const int MaxTasksPerTick = 100;

        /// <summary>
        /// Warning threshold in milliseconds. If tick processing exceeds this, publish a warning event.
        /// </summary>
        private const long WarningThresholdMs = 80;

        /// <summary>
        /// Number of tasks executed during the last tick.
        /// </summary>
        private int _totalTasksProcessedLastTick = 0;

        /// <summary>
        /// Maximum processing time observed across all ticks (in milliseconds).
        /// </summary>
        private long _maxProcessingTimeMs = 0;

        /// <summary>
        /// Number of tasks deferred due to budget enforcement.
        /// </summary>
        private int _totalTasksDeferred = 0;

        public long CurrentTick => _currentTick;

        /// <summary>
        /// Gets the number of tasks executed during the last tick.
        /// </summary>
        public int TotalTasksProcessedLastTick => _totalTasksProcessedLastTick;

        /// <summary>
        /// Gets the maximum processing time observed across all ticks (in milliseconds).
        /// </summary>
        public long MaxProcessingTimeMs => _maxProcessingTimeMs;

        /// <summary>
        /// Gets the total number of tasks deferred due to budget enforcement.
        /// </summary>
        public int TotalTasksDeferred => _totalTasksDeferred;

        public TickTaskScheduler(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _tickSubscription = _eventBus.Subscribe<TickEvents.Timing.GameTickEvent>(
                EventMessageType.System,
                OnGameTick
            );
        }

        public IScheduledTask ScheduleAfter(long tickDelta, Action action)
        {
            if (tickDelta < 0) throw new ArgumentOutOfRangeException(nameof(tickDelta));
            return ScheduleAt(_currentTick + tickDelta, action);
        }

        public IScheduledTask ScheduleAt(long targetTick, Action action)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TickTaskScheduler));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var task = new ScheduledTask(targetTick, action, recurring: false, interval: 0);

            if (!_taskQueue.ContainsKey(targetTick))
            {
                _taskQueue[targetTick] = new List<ScheduledTask>();
            }

            _taskQueue[targetTick].Add(task);
            return task;
        }

        public IScheduledTask ScheduleRecurring(long interval, Action action)
        {
            if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var task = new ScheduledTask(_currentTick + interval, action, recurring: true, interval: interval);

            if (!_taskQueue.ContainsKey(task.ScheduledTick))
            {
                _taskQueue[task.ScheduledTick] = new List<ScheduledTask>();
            }

            _taskQueue[task.ScheduledTick].Add(task);
            return task;
        }

        private void OnGameTick(TickEvents.Timing.GameTickEvent evt)
        {
            _currentTick = evt.TickNumber;

            var stopwatch = Stopwatch.StartNew();
            int tasksExecuted = 0;
            int tasksDeferredThisTick = 0;
            bool budgetExceeded = false;

            // Find all ticks that should execute now or earlier
            var ticksToProcess = new List<long>();

            foreach (var tick in _taskQueue.Keys)
            {
                if (tick <= _currentTick)
                {
                    ticksToProcess.Add(tick);
                }
                else
                {
                    break; // SortedDictionary is ordered, so we can stop early
                }
            }

            // Process tasks from oldest to newest tick
            foreach (var tick in ticksToProcess)
            {
                // Check if we've exceeded the task budget
                if (tasksExecuted >= MaxTasksPerTick)
                {
                    budgetExceeded = true;
                    // Count remaining tasks as deferred
                    foreach (var remainingTick in ticksToProcess)
                    {
                        if (remainingTick >= tick && _taskQueue.ContainsKey(remainingTick))
                        {
                            tasksDeferredThisTick += _taskQueue[remainingTick].Count;
                        }
                    }
                    break;
                }

                var tasks = _taskQueue[tick];
                _taskQueue.Remove(tick);

                foreach (var task in tasks)
                {
                    // Check budget before each task
                    if (tasksExecuted >= MaxTasksPerTick)
                    {
                        budgetExceeded = true;
                        tasksDeferredThisTick++;

                        // Re-schedule deferred task for next tick
                        var deferredTick = _currentTick + 1;
                        task.UpdateScheduledTick(deferredTick);

                        if (!_taskQueue.ContainsKey(deferredTick))
                        {
                            _taskQueue[deferredTick] = new List<ScheduledTask>();
                        }

                        _taskQueue[deferredTick].Add(task);
                        continue;
                    }

                    if (!task.IsCancelled)
                    {
                        try
                        {
                            task.Execute();
                            tasksExecuted++;
                        }
                        catch (Exception ex)
                        {
                            // Publish error event but continue processing other tasks
                            _eventBus.Publish(
                                EventMessageType.System,
                                new TickEvents.Errors.ScheduledTaskExecutionFailed(_currentTick, task.ScheduledTick, ex)
                            );
                        }

                        // Reschedule recurring tasks
                        if (task.IsRecurring && !task.IsCancelled)
                        {
                            var nextTick = _currentTick + task.Interval;
                            task.UpdateScheduledTick(nextTick);

                            if (!_taskQueue.ContainsKey(nextTick))
                            {
                                _taskQueue[nextTick] = new List<ScheduledTask>();
                            }

                            _taskQueue[nextTick].Add(task);
                        }
                    }
                }
            }

            stopwatch.Stop();

            // Update diagnostics
            _totalTasksProcessedLastTick = tasksExecuted;
            _totalTasksDeferred += tasksDeferredThisTick;
            _maxProcessingTimeMs = Math.Max(_maxProcessingTimeMs, stopwatch.ElapsedMilliseconds);

            // Publish diagnostic events
            if (budgetExceeded)
            {
                _eventBus.Publish(
                    EventMessageType.System,
                    new TickEvents.Warnings.TickBudgetExceeded(_currentTick, tasksExecuted, tasksDeferredThisTick)
                );
            }

            if (stopwatch.ElapsedMilliseconds >= WarningThresholdMs)
            {
                _eventBus.Publish(
                    EventMessageType.System,
                    new TickEvents.Warnings.TickProcessingSlow(_currentTick, stopwatch.ElapsedMilliseconds, tasksExecuted)
                );
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _tickSubscription?.Dispose();
            _taskQueue.Clear();

            _disposed = true;
        }
    }
}