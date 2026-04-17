using System.Text;

namespace Shared.Logging
{
    /// <summary>
    /// Standard file writer implementation for logging.
    /// Writes log entries to a text file with UTF-8 encoding and auto-flush enabled.
    /// </summary>
    public sealed class StandardLogFileWriter : ILogFileWriter
    {
        private readonly StreamWriter _writer;
        private readonly object _writeLock = new();
        private bool _disposed;

        public StandardLogFileWriter(string filePath, bool append = true)
        {
            _writer = new StreamWriter(filePath, append, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public void WriteLine(string line)
        {
            lock (_writeLock)
            {
                if (_disposed) return;
                _writer.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                if (_disposed) return;
                _disposed = true;
                _writer.Dispose();
            }
        }
    }
}
