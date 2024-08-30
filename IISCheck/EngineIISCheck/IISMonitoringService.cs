using IISCheck.Model;
using Logger;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;


namespace IISCheck.EngineIISCheck
{
    public class IISMonitoringService
    {
        private readonly Logger.Logger _logger;
        private System.Timers.Timer _timer;
        public event Action<List<IISApplicationPoolModel>> OnMonitoringDataUpdated;

        public IISMonitoringService(string logFilePath)
        {
            // Initialize the logger with the provided log file path
            ILogRepository logRepository = new FileLogRepository(logFilePath);
            _logger = new Logger.Logger(logRepository);
        }

        public void StartRealTimeMonitoring(int interval)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _logger.LogInfo("Real-time monitoring started.");
        }
        public void StartRealTimeMonitoringForSpecificPool(string poolName, int interval)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += (sender, e) => MonitorSpecificApplicationPool(poolName);
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _logger.LogInfo($"Real-time monitoring started for Application Pool '{poolName}'.");
        }

        public void StopRealTimeMonitoring()
        {
            if (_timer != null && _timer.Enabled)
            {
                _timer.Stop();
                _timer.Dispose();
                _logger.LogInfo("Real-time monitoring stopped.");
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MonitorApplicationPools();

        }

        private void MonitorApplicationPools()
        {
            List<IISApplicationPoolModel> appPoolModels = new List<IISApplicationPoolModel>();

            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    foreach (ApplicationPool appPool in serverManager.ApplicationPools)
                    {
                        int processId = appPool.WorkerProcesses.Count > 0 ? appPool.WorkerProcesses[0].ProcessId : 0;

                        IISApplicationPoolModel model = new IISApplicationPoolModel
                        {
                            Name = appPool.Name,
                            Status = appPool.State.ToString(),
                            ProcessId = processId,
                            CPUUsage = processId > 0 ? GetCpuUsage(processId) : 0,
                            MemoryUsage = GetMemoryUsage(processId),
                            RequestCount = appPool.WorkerProcesses.Count > 0 ? appPool.WorkerProcesses[0].GetRequests(0).Count() : 0,
                            PipelineMode = appPool.ManagedPipelineMode.ToString(),
                            AutoStart = appPool.AutoStart,
                            IdentityType = appPool.ProcessModel.IdentityType.ToString(),
                            IdleTimeout = appPool.ProcessModel.IdleTimeout,
                            MaxProcesses = appPool.ProcessModel.MaxProcesses
                        };

                        appPoolModels.Add(model);

                        //_logger.LogInfo($"Successfully monitored Application Pool: {appPool.Name}");
                    }

                    // Raise the event with the updated data
                    OnMonitoringDataUpdated?.Invoke(appPoolModels);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during IIS Application Pool monitoring: {ex.Message}");
            }
        }

        private long GetMemoryUsage(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    long memoryUsage = process.WorkingSet64;
                   // _logger.LogInfo($"Memory usage for Process ID {processId}: {memoryUsage} bytes");
                    return memoryUsage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving memory usage for Process ID {processId}: {ex.Message}");
                return 0;
            }
        }

        private double GetCpuUsage(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    TimeSpan startCpuUsage = process.TotalProcessorTime;
                    DateTime startTime = DateTime.Now;

                    System.Threading.Thread.Sleep(500);

                    TimeSpan endCpuUsage = process.TotalProcessorTime;
                    DateTime endTime = DateTime.Now;

                    double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    double totalMsPassed = (endTime - startTime).TotalMilliseconds;

                    double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;

                   // _logger.LogInfo($"CPU usage for Process ID {processId}: {cpuUsageTotal:F2}%");

                    return cpuUsageTotal;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving CPU usage for Process ID {processId}: {ex.Message}");
                return 0;
            }
        }

        private void MonitorSpecificApplicationPool(string specificPoolName)
        {
            List<IISApplicationPoolModel> appPoolModels = new List<IISApplicationPoolModel>();

            try
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    ApplicationPool appPool = serverManager.ApplicationPools[specificPoolName];

                    if (appPool == null)
                    {
                        _logger.LogError($"Application Pool '{specificPoolName}' not found.");
                        return;
                    }

                    int processId = appPool.WorkerProcesses.Count > 0 ? appPool.WorkerProcesses[0].ProcessId : 0;

                    IISApplicationPoolModel model = new IISApplicationPoolModel
                    {
                        Name = appPool.Name,
                        Status = appPool.State.ToString(),
                        ProcessId = processId,
                        CPUUsage = processId > 0 ? GetCpuUsage(processId) : 0,
                        MemoryUsage = GetMemoryUsage(processId),
                        RequestCount = appPool.WorkerProcesses.Count > 0 ? appPool.WorkerProcesses[0].GetRequests(0).Count() : 0,
                        PipelineMode = appPool.ManagedPipelineMode.ToString(),
                        AutoStart = appPool.AutoStart,
                        IdentityType = appPool.ProcessModel.IdentityType.ToString(),
                        IdleTimeout = appPool.ProcessModel.IdleTimeout,
                        MaxProcesses = appPool.ProcessModel.MaxProcesses
                    };

                    appPoolModels.Add(model);

                    // Check if the specific application pool is stopped and auto-start it
                    if (appPool.State == ObjectState.Stopped)
                    {
                        _logger.LogWarning($"Application Pool '{specificPoolName}' is stopped. Attempting to start it.");
                        appPool.Start();
                        _logger.LogInfo($"Application Pool '{specificPoolName}' has been started automatically.");
                    }

                    // Raise an event to update the UI
                    OnMonitoringDataUpdated?.Invoke(appPoolModels);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during monitoring of Application Pool '{specificPoolName}': {ex.Message}");
            }
        }

    }
}
