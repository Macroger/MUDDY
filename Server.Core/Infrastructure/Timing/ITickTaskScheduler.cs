namespace Server.Core.Infrastructure.Timing
{
    public interface ITickTaskScheduler : IDisposable
    {
        /// <summary>
        /// Schedules an action to execute after a specified number of ticks from now.
        /// </summary>
        /// <param name="tickDelta">Number of ticks to wait before executing.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>A handle that can be used to cancel the scheduled task.</returns>
        IScheduledTask ScheduleAfter(long tickDelta, Action action);

        /// <summary>
        /// Schedules an action to execute at a specific absolute tick count.
        /// </summary>
        /// <param name="targetTick">The exact tick number at which to execute.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>A handle that can be used to cancel the scheduled task.</returns>
        IScheduledTask ScheduleAt(long targetTick, Action action);

        /// <summary>
        /// Schedules a recurring action that executes every N ticks.
        /// </summary>
        /// <param name="interval">Number of ticks between executions.</param>
        /// <param name="action">The action to execute each interval.</param>
        /// <returns>A handle that can be used to cancel the recurring task.</returns>
        IScheduledTask ScheduleRecurring(long interval, Action action);

        /// <summary>
        /// Gets the current tick count known to the scheduler.
        /// </summary>
        long CurrentTick { get; }
    }
}
