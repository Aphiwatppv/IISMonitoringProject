using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public interface ILogRepository
    {
        void AddLog(LogEntry logEntry);
        IEnumerable<LogEntry> GetAllLogs();
        IEnumerable<LogEntry> GetLogsByLevel(string logLevel);
    }
}
