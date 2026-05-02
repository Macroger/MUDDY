// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;
using System.Text;

namespace Shared.Logging
{
    /// <summary>
    /// Standard file writer implementation for logging.
    /// Writes log entries to a text file with UTF-8 encoding and auto-flush enabled.
    /// </summary>
    public sealed class StandardLogFileWriter : ILogFileWriter
    {
        private readonly StreamWriter? _writer;
        private readonly object _writeLock = new();
        private bool _disposed;
        private readonly string _filePath;
        private bool _loggedFailure = false;

        public StandardLogFileWriter(string filePath, bool append = true)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _writer = new StreamWriter(filePath, append, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // Write a startup message to confirm logging is working
                _writer.WriteLine($"=== Log file created at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch (Exception ex)
            {
                // Log to console/debug output but don't crash the application
                Console.WriteLine($"ERROR: Failed to create log file at '{filePath}': {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to create log file at '{filePath}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // _writer remains null, WriteLine will handle gracefully
                _loggedFailure = true;
            }
        }

        public void WriteLine(string line)
        {
            lock (_writeLock)
            {
                if (_disposed) return;

                if (_writer == null)
                {
                    // Only log once to avoid spam
                    if (!_loggedFailure)
                    {
                        Console.WriteLine($"WARNING: Cannot write to log file '{_filePath}' - writer not initialized");
                        _loggedFailure = true;
                    }
                    return;
                }

                try
                {
                    _writer.WriteLine(line);
                }
                catch (Exception ex)
                {
                    // Log to console but don't crash
                    if (!_loggedFailure)
                    {
                        Console.WriteLine($"ERROR: Failed to write to log file '{_filePath}': {ex.Message}");
                        _loggedFailure = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    _writer?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WARNING: Error disposing log file writer for '{_filePath}': {ex.Message}");
                }
            }
        }
    }
}
