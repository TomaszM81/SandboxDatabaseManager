using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SandboxDatabaseManager.Worker.SignalR
{
    [HubName("monitoringHub")]
    public class MonitoringHub: Hub
    {
        private readonly MonitoringBackgroundWorker instance;

        public MonitoringHub() : this(MonitoringBackgroundWorker.Instance) { }

        public MonitoringHub(MonitoringBackgroundWorker instance)
        {
            this.instance = instance;
        }

        public IEnumerable<SandboxDatabaseManager.Worker.MonitoringBackgroundWorker.ServerPerformanceCounterStats> GetCounterStats()
        {
            return instance.CurrentStats;
        }
    }
}