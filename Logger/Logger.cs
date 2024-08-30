using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class Logger
    {
        private readonly ILogRepository _logRepository;

        public Logger(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        private void Log(string logLevel, string message)
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                LogLevel = logLevel,
                Message = message
            };

            _logRepository.AddLog(logEntry);
        }
    }

}
