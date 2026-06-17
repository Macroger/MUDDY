namespace Server.Core.Infrastructure.Timing
{
    /// <summary>
    /// Represents a scheduled task that can be cancelled before execution.
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// The tick at which this task is scheduled to execute.
        /// </summary>
        long ScheduledTick { get; }

        /// <summary>
        /// Whether this task has been cancelled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Whether this task has already executed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Cancels the scheduled task if it hasn't executed yet.
        /// </summary>
        void Cancel();
    }
}

