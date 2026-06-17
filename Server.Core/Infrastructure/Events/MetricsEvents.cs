// =============================================================================
/// @file       MetricsEvents.cs
/// @namespace  Server.Core.Infrastructure.Events
/// @brief      Domain events published by the metrics collection system.
///             Includes notifications and errors related to performance metrics.
// =============================================================================

using Shared.EventBus.EventTypes;
using Shared.Logging;
using Server.Core.Infrastructure.Metrics;

namespace Server.Core.Infrastructure.Events
{
    /// <summary>
    /// Container for all metrics-related domain events.
    /// </summary>
    public static class MetricsEvents
    {
        /// <summary>
        /// Notifications published when metrics are collected or errors occur.
        /// </summary>
        public static class Notifications
        {
            /// <summary>
            /// Published when a new metric sample is collected.
            /// Subscribed by GUI for real-time visualization.
            /// </summary>
            /// <param name="Sample">The metric sample that was collected.</param>
            public sealed record MetricSampleCollected(MetricSample Sample) : BusEvent(EventMessageType.System, LogLevel.Trace);
        }

        /// <summary>
        /// Errors published when metrics collection fails.
        /// </summary>
        public static class Errors
        {
            /// <summary>
            /// Published when an error occurs during metrics collection.
            /// </summary>
            /// <param name="ErrorMessage">A message describing the error that occurred.</param>
            /// <param name="Exception">The exception object associated with the error, if available.</param>
            public sealed record MetricsError(string ErrorMessage, Exception? Exception = null) : BusEvent(EventMessageType.System, LogLevel.Error);
        }
    }
}