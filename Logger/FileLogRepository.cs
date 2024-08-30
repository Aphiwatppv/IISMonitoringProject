using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class FileLogRepository : ILogRepository
    {
        private readonly string _logFilePath;

        public FileLogRepository(string logFileName)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(currentDirectory, logFileName);
        }

        public void AddLog(LogEntry logEntry)
        {
            string logLine = $"{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss} [{logEntry.LogLevel}] {logEntry.Message}";
            File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
        }

        public IEnumerable<LogEntry> GetAllLogs()
        {
            if (!File.Exists(_logFilePath))
            {
                return Enumerable.Empty<LogEntry>();
            }

            return File.ReadAllLines(_logFilePath)
                       .Select(ParseLogLine)
                       .Where(log => log != null);
        }

        public IEnumerable<LogEntry> GetLogsByLevel(string logLevel)
        {
            return GetAllLogs().Where(log => log.LogLevel == logLevel);
        }

        private LogEntry ParseLogLine(string logLine)
        {
            try
            {
                var parts = logLine.Split(new[] { ' ' }, 4);
                return new LogEntry
                {
                    Timestamp = DateTime.Parse($"{parts[0]} {parts[1]}"),
                    LogLevel = parts[2].Trim('[', ']'),
                    Message = parts[3]
                };
            }
            catch
            {
                // Handle parsing errors or return null for invalid lines
                return null;
            }
        }
    }
}
