namespace Shared.Logging
{
    /// <summary>
    /// Interface for writing log entries to a file.
    /// Abstracts file I/O operations for logging systems.
    /// </summary>
    public interface ILogFileWriter : IDisposable
    {
        /// <summary>
        /// Writes a line of text to the log file.
        /// </summary>
        /// <param name="line">The text line to write to the file.</param>
        void WriteLine(string line);
    }
}
