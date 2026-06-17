// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using System;
using System.IO;

namespace Shared.Logging
{
    /// <summary>
    /// Provides safe, user-writable paths for log files.
    /// Uses Environment.SpecialFolder.LocalApplicationData to ensure logs are written 
    /// to a location that doesn't require elevated permissions.
    /// </summary>
    public static class LogPathHelper
    {
        private const string AppFolderName = "MUDDY";

        /// <summary>
        /// Gets the base directory for application logs.
        /// Example: C:\Users\[Username]\AppData\Local\MUDDY\Logs
        /// </summary>
        public static string GetLogDirectory()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDirectory = Path.Combine(appDataPath, AppFolderName, "Logs");

                // Ensure directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                return logDirectory;
            }
            catch (Exception ex)
            {
                // Fallback to temp directory if AppData is not accessible
                Console.WriteLine($"Warning: Could not access LocalApplicationData. Using temp directory. Error: {ex.Message}");
                string tempLogDir = Path.Combine(Path.GetTempPath(), AppFolderName, "Logs");
                Directory.CreateDirectory(tempLogDir);
                return tempLogDir;
            }
        }

        /// <summary>
        /// Gets a full path for a log file in the application log directory.
        /// </summary>
        /// <param name="fileName">The log file name (e.g., "client_packets_2026-01-15.log")</param>
        /// <returns>Full path to the log file in a safe, writable location</returns>
        public static string GetLogFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Log file name cannot be null or empty", nameof(fileName));
            }

            string logDirectory = GetLogDirectory();
            return Path.Combine(logDirectory, fileName);
        }

        /// <summary>
        /// Creates a timestamped log file path.
        /// </summary>
        /// <param name="prefix">Prefix for the log file (e.g., "client_packets", "server_packets")</param>
        /// <returns>Full path with timestamp</returns>
        public static string CreateTimestampedLogPath(string prefix)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{prefix}_{timestamp}.log";
            return GetLogFilePath(fileName);
        }
    }
}
