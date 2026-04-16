namespace Shared.Protocol.Types
{
    /// <summary>
    /// Indicates the result of a processing step or operation.
    /// </summary>
    public enum ProcessOutcome
    {
        /// <summary>The processing step completed successfully.</summary>
        Succeeded,
        /// <summary>The processing step failed.</summary>
        Failed,
        /// <summary>The processing step was skipped by design or due to preconditions.</summary>
        Skipped
    }
}
