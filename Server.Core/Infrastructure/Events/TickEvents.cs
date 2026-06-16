// =============================================================================
/// @file       TickEvents.cs
/// @namespace  Server.Core.Infrastructure.Events
/// @brief      Timing and tick-related events for server game loop coordination.
/// @details    Published by TickSystem and TickTaskScheduler; consumed by
///             subsystems that need regular update cycles and diagnostics.
// =============================================================================
using Shared.EventBus.EventTypes;
using Shared.Logging;
using System;

namespace Server.Core.Infrastructure.Events
{
    public class TickEvents
    {
        public class Timing
        {
            /// <summary>
            /// Published every game tick by the TickSystem. Subscribers use this to
            /// drive periodic updates (AI, combat, resource regeneration, etc.).
            /// </summary>
            /// <param name="TickNumber">Monotonically increasing tick counter.</param>
            /// <param name="DeltaTime">Time elapsed since the last tick, in milliseconds.</param>
            public sealed record GameTickEvent(long TickNumber, double DeltaTime)
                : BusEvent(EventMessageType.System, LogLevel.Debug);
        }

        public class Warnings
        {
            /// <summary>
            /// Published when the tick task scheduler exceeds its per-tick task budget.
            /// Some tasks were deferred to the next tick to prevent overload.
            /// </summary>
            /// <param name="TickNumber">The tick during which the budget was exceeded.</param>
            /// <param name="TasksExecuted">Number of tasks that were executed before hitting the budget.</param>
            /// <param name="TasksDeferred">Number of tasks deferred to the next tick.</param>
            public sealed record TickBudgetExceeded(long TickNumber, int TasksExecuted, int TasksDeferred)
                : BusEvent(EventMessageType.System, LogLevel.Warning);

            /// <summary>
            /// Published when tick processing takes longer than the warning threshold.
            /// Indicates potential performance issues or excessive task load.
            /// </summary>
            /// <param name="TickNumber">The tick that took too long to process.</param>
            /// <param name="ProcessingTimeMs">How long the tick processing took, in milliseconds.</param>
            /// <param name="TasksExecuted">Number of tasks executed during this tick.</param>
            public sealed record TickProcessingSlow(long TickNumber, long ProcessingTimeMs, int TasksExecuted)
                : BusEvent(EventMessageType.System, LogLevel.Warning);
        }

        public class Errors
        {
            /// <summary>
            /// Published when a scheduled task throws an exception during execution.
            /// The scheduler continues processing other tasks after logging this error.
            /// </summary>
            /// <param name="CurrentTick">The tick during which the error occurred.</param>
            /// <param name="ScheduledTick">The tick the failed task was originally scheduled for.</param>
            /// <param name="Exception">The exception thrown by the task.</param>
            public sealed record ScheduledTaskExecutionFailed(long CurrentTick, long ScheduledTick, Exception Exception)
                : BusEvent(EventMessageType.System, LogLevel.Error);
        }
    }
}