// =============================================================================
/// @file       IMetricsCollector.cs
/// @namespace  Server.Core.Infrastructure.Metrics
/// @brief      Interface for collecting and retrieving server performance metrics.
///             Publishes domain events via the event bus.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Server.Core.Infrastructure.Metrics
{
    /// <summary>
    /// Interface for collecting runtime metrics (tick time, event throughput, memory, etc.)
    /// from the game loop and publishing them via the event bus.
    /// </summary>
    public interface IMetricsCollector : IDisposable
    {
        /// <summary>
        /// Starts sampling metrics at the configured interval.
        /// </summary>
        void StartSampling();

        /// <summary>
        /// Stops sampling and clears historical data.
        /// </summary>
        void StopSampling();

        /// <summary>
        /// Returns all metric samples within the specified time range.
        /// Returns in chronological order (oldest first).
        /// </summary>
        /// <param name="from">Start of time range (inclusive).</param>
        /// <param name="to">End of time range (inclusive).</param>
        /// <returns>List of metric samples within the range.</returns>
        IReadOnlyList<MetricSample> GetMetricsInRange(DateTime from, DateTime to);

        /// <summary>
        /// Returns the most recent N samples.
        /// </summary>
        /// <param name="count">Number of samples to retrieve.</param>
        /// <returns>List of most recent samples, in chronological order.</returns>
        IReadOnlyList<MetricSample> GetRecentSamples(int count);

        /// <summary>
        /// Sets the sampling interval. Default: 1 second.
        /// </summary>
        /// <param name="interval">Time between samples.</param>
        void SetSamplingInterval(TimeSpan interval);
    }
}