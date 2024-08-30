using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IISCheck.Model
{
    public class IISApplicationPoolModel
    {
        public string Name { get; set; } // The name of the application pool
        public string Status { get; set; } // The current status of the application pool (e.g., Started, Stopped)
        public DateTime StartTime { get; set; } // The time the application pool was started
        public long ProcessId { get; set; } // The process ID of the application pool's worker process
        public double CPUUsage { get; set; } // The CPU usage of the application pool (percentage)
        public long MemoryUsage { get; set; } // The memory usage of the application pool (in bytes)
        public int RequestCount { get; set; } // The total number of requests handled by the application pool
        public int QueueLength { get; set; } // The current request queue length for the application pool
        public string PipelineMode { get; set; } // The pipeline mode (e.g., Integrated, Classic)
        public bool AutoStart { get; set; } // Whether the application pool is set to start automatically
        public string IdentityType { get; set; } // The identity type under which the application pool is running
        public TimeSpan IdleTimeout { get; set; } // The idle timeout period for the application pool
        public long MaxProcesses { get; set; } // The maximum number of worker processes in the application pool
        public DateTime date_time { get; set; } = DateTime.Now;
        public string MachineName {  get; set; } = Environment.MachineName;
        public string version {  get; set; } = Environment.Version.ToString();
    }

}
