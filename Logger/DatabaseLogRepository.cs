using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    internal class DatabaseLogRepository : ILogRepository
    {
        private readonly string _connectionString;

        public DatabaseLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddLog(LogEntry logEntry)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("INSERT INTO Logs (Timestamp, LogLevel, Message) VALUES (@Timestamp, @LogLevel, @Message)", connection);
                command.Parameters.AddWithValue("@Timestamp", logEntry.Timestamp);
                command.Parameters.AddWithValue("@LogLevel", logEntry.LogLevel);
                command.Parameters.AddWithValue("@Message", logEntry.Message);
                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<LogEntry> GetAllLogs()
        {
            var logs = new List<LogEntry>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, Timestamp, LogLevel, Message FROM Logs", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new LogEntry
                        {
                            Id = reader.GetInt32(0),
                            Timestamp = reader.GetDateTime(1),
                            LogLevel = reader.GetString(2),
                            Message = reader.GetString(3)
                        });
                    }
                }
            }
            return logs;
        }

        public IEnumerable<LogEntry> GetLogsByLevel(string logLevel)
        {
            return GetAllLogs().Where(log => log.LogLevel == logLevel);
        }
    }
}
