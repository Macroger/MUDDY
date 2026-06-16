// =============================================================================
/// @file       MetricSample.cs
/// @namespace  Server.Core.Infrastructure.Metrics
/// @brief      A single point-in-time measurement of server performance.
// =============================================================================

namespace Server.Core.Infrastructure.Metrics
{
    /// <summary>
    /// Represents a single sampled metric at a specific point in time.
    /// Used for time-series visualization in the server GUI.
    /// </summary>
    /// <remarks>
    /// Properties use <c>set</c> rather than <c>init</c> to satisfy the WinUI XAML
    /// type system, which generates code requiring a parameterless constructor
    /// and settable properties for all data-bound types.
    /// </remarks>
    public sealed record MetricSample
    {
        /// <summary>Timestamp when this sample was taken.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Time in milliseconds taken to process the last game tick.</summary>
        public double TickTimeMs { get; set; }

        /// <summary>Number of domain events processed during the sample interval.</summary>
        public int EventsProcessed { get; set; }

        /// <summary>Number of active players connected at this sample time.</summary>
        public int ActivePlayers { get; set; }

        /// <summary>Approximate memory usage in megabytes.</summary>
        public double MemoryUsageMb { get; set; }
    }
}