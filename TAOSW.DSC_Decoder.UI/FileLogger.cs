using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAOSW.DSC_Decoder.Core.Interfaces;

namespace TAOSW.DSC_Decoder.UI
{
    /// <summary>
    /// File-based logger implementation that creates daily log files
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();
        private readonly string _fileNamePrefix;
        private string? _currentLogFilePath;
        private DateTime _currentLogDate;

        /// <summary>
        /// Initializes a new instance of FileLogger
        /// </summary>
        /// <param name="logDirectory">Directory where log files will be stored (optional, defaults to "Logs")</param>
        /// <param name="fileNamePrefix">Prefix for log file names (optional, defaults to "DSC_Log")</param>
        public FileLogger(string? logDirectory = null, string? fileNamePrefix = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _fileNamePrefix = fileNamePrefix ?? "DSC_Log";
            _currentLogDate = DateTime.MinValue; // Force initial file creation
            
            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);
            
            // Log initialization (will be written to first log file created)
            WriteLog("INFO", $"FileLogger initialized - Directory: {_logDirectory}");
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="ex">Optional exception to include in the log</param>
        public void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message} | StackTrace: {ex.StackTrace}"
                : message;
            
            WriteLog("ERROR", fullMessage);
        }

        /// <summary>
        /// Core method that writes log entries to the appropriate daily file
        /// </summary>
        /// <param name="level">Log level (INFO, WARNING, ERROR)</param>
        /// <param name="message">Message to log</param>
        private void WriteLog(string level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var now = DateTime.Now;
                    var logDate = now.Date;
                    
                    // Check if we need to create a new log file for today
                    if (_currentLogFilePath == null || _currentLogDate != logDate)
                    {
                        CreateNewLogFile(logDate);
                    }

                    // Format: 2025-01-15 14:30:25.123 - INFO - Message content
                    var logEntry = $"{now:yyyy-MM-dd HH:mm:ss.fff} - {level} - {message}";
                    
                    // Write to file
                    File.AppendAllText(_currentLogFilePath!, logEntry + Environment.NewLine, Encoding.UTF8);
                    
                    // Also output to console for debugging
                    // Console logging removed - all logging now goes through FileLogger only
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails - this is the only Console.WriteLine kept for critical fallback
                Console.WriteLine($"[LOG ERROR] Failed to write to log file: {ex.Message}");
                Console.WriteLine($"[LOG FALLBACK] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {level} - {message}");
            }
        }

        /// <summary>
        /// Creates a new log file for the specified date
        /// </summary>
        /// <param name="logDate">Date for the log file</param>
        private void CreateNewLogFile(DateTime logDate)
        {
            try
            {
                _currentLogDate = logDate;
                var fileName = $"{_fileNamePrefix}_{logDate:yyyy-MM-dd}.log";
                _currentLogFilePath = Path.Combine(_logDirectory, fileName);
                
                // Create file if it doesn't exist and write header
                if (!File.Exists(_currentLogFilePath))
                {
                    var header = $"=== DSC Decoder Log File - {logDate:yyyy-MM-dd} ==={Environment.NewLine}" +
                                $"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                                $"Application: TAOSW DSC Decoder{Environment.NewLine}" +
                                $"Log Format: DateTime - LEVEL - Message{Environment.NewLine}" +
                                $"================================================{Environment.NewLine}";
                    
                    File.WriteAllText(_currentLogFilePath, header, Encoding.UTF8);
                    // Log file creation logged through normal logging system now
                }
                // File already exists - no logging needed to avoid circular logging
            }
            catch (Exception ex)
            {
                // Critical error - keep console fallback for file creation issues
                Console.WriteLine($"Error creating log file: {ex.Message}");
                _currentLogFilePath = null;
            }
        }

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        public string? CurrentLogFilePath 
        { 
            get 
            { 
                lock (_lockObject) 
                { 
                    return _currentLogFilePath; 
                } 
            } 
        }

        /// <summary>
        /// Gets the log directory path
        /// </summary>
        public string LogDirectory => _logDirectory;

        /// <summary>
        /// Manually forces creation of a new log file for today (useful for testing)
        /// </summary>
        public void ForceNewLogFile()
        {
            lock (_lockObject)
            {
                CreateNewLogFile(DateTime.Now.Date);
            }
        }

        /// <summary>
        /// Gets a list of all existing log files in the log directory
        /// </summary>
        /// <returns>Array of log file paths</returns>
        public string[] GetExistingLogFiles()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return Array.Empty<string>();

                return Directory.GetFiles(_logDirectory, $"{_fileNamePrefix}_*.log")
                               .OrderByDescending(f => f) // Most recent first
                               .ToArray();
            }
            catch (Exception ex)
            {
                // Critical error accessing file system - keep console fallback
                Console.WriteLine($"Error getting log files: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Cleans up old log files, keeping only the specified number of days
        /// </summary>
        /// <param name="daysToKeep">Number of days of logs to keep (default: 30)</param>
        /// <returns>Number of files deleted</returns>
        public int CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.Date.AddDays(-daysToKeep);
                var logFiles = GetExistingLogFiles();
                var deletedCount = 0;

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var datePart = fileName.Substring(_fileNamePrefix.Length + 1); // +1 for underscore
                        
                        if (DateTime.TryParseExact(datePart, "yyyy-MM-dd", null, 
                            System.Globalization.DateTimeStyles.None, out var fileDate))
                        {
                            if (fileDate < cutoffDate)
                            {
                                File.Delete(file);
                                deletedCount++;
                                // Removed Console.WriteLine - cleanup details logged through normal logging
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log deletion errors through normal logging since this is not critical to system function
                        WriteLog("WARNING", $"Error deleting log file {file}: {ex.Message}");
                    }
                }

                LogInfo($"Log cleanup completed. Deleted {deletedCount} old log files (keeping {daysToKeep} days).");
                return deletedCount;
            }
            catch (Exception ex)
            {
                LogError("Error during log cleanup", ex);
                return 0;
            }
        }

        /// <summary>
        /// Writes a startup message to the log
        /// </summary>
        public void LogStartup()
        {
            LogInfo("=== APPLICATION STARTUP ===");
            LogInfo($"TAOSW DSC Decoder started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo($"Log file: {CurrentLogFilePath}");
            LogInfo($"Application directory: {AppDomain.CurrentDomain.BaseDirectory}");
        }

        /// <summary>
        /// Writes a shutdown message to the log
        /// </summary>
        public void LogShutdown()
        {
            LogInfo($"TAOSW DSC Decoder shutting down at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("=== APPLICATION SHUTDOWN ===");
        }
    }
}
