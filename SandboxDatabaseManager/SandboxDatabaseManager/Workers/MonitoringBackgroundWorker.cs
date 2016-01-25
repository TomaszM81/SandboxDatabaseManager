using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SandboxDatabaseManager.SandboxDatabaseManagerService;
using SandboxDatabaseManager.Configuration;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using SandboxDatabaseManager.Worker.SignalR;
using System.Data;
using System.Threading;
using Newtonsoft.Json;

namespace SandboxDatabaseManager.Worker
{
    public class MonitoringBackgroundWorker
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Lazy<MonitoringBackgroundWorker> _instance = new Lazy<MonitoringBackgroundWorker>(() => new MonitoringBackgroundWorker(GlobalHost.ConnectionManager.GetHubContext<MonitoringHub>().Clients));
        private Task backgroundTask;
        public List<Tuple<string, SandboxDatabaseManagerServiceClient>> MyMonitoredServers;
        private object sync = new object();
        private List<ServerPerformanceCounterStats> currentStats = new List<ServerPerformanceCounterStats>();
        private TimeSpan _storeCounterDataEach = new TimeSpan(0, 5, 0);
        private TimeSpan _removeOldCounterDataEach = new TimeSpan(24, 0, 0);
        private DateTime _lastMaxStored = DateTime.MinValue.AddHours(24);
        private DateTime _lastRemoveOldCounterDataEach = DateTime.MinValue;
        private DataTable _statsToStore;
        private DataTable _countersDataMinDates;
        private ReaderWriterLockSlim _section = new ReaderWriterLockSlim();
        private IHubConnectionContext<dynamic> Clients
        {
            get;
            set;
        }
        private MonitoringBackgroundWorker(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
            _statsToStore = new DataTable();
            _statsToStore.Columns.Add("ServerFriendlyName", typeof(string));
            _statsToStore.Columns.Add("CounterFriendlyName", typeof(string));
            _statsToStore.Columns.Add("CounterValue", typeof(double));
            _statsToStore.Columns.Add("CounterValueDate", typeof(DateTime));


            SandboxDatabaseManagerServiceClient client;

            MyMonitoredServers = new List<Tuple<string, SandboxDatabaseManagerServiceClient>>();
            foreach (var serverToMonitor in MonitoredServers.Instance.ItemsList)
            {
                try
                {
                    client = new SandboxDatabaseManagerServiceClient("NetTcpBinding_ISandboxDatabaseManagerService", serverToMonitor.RemoteAddress);
                    MyMonitoredServers.Add(new Tuple<string, SandboxDatabaseManagerServiceClient>(serverToMonitor.FriendlyName, client));
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Failed to construct SandboxDatabaseManagerServiceClient for client FriendlyName: {0}, RemotAddress: {1}", serverToMonitor.FriendlyName, serverToMonitor.RemoteAddress), ex);
                }
            }

            backgroundTask = new Task(MonitorAndPushStatistics, TaskCreationOptions.LongRunning);
            backgroundTask.Start();

        }
        private async void MonitorAndPushStatistics()
        {
            object sync = new object();
            List<ServerPerformanceCounterStats> stats;

            while (true)
            {
                stats = new List<ServerPerformanceCounterStats>();

                List<Tuple<string, SandboxDatabaseManagerServiceClient>> failedRemoteServers = new List<Tuple<string, SandboxDatabaseManagerServiceClient>>();

                Parallel.ForEach(MyMonitoredServers, (serverClinent, LoopState) =>
                {
                    try
                    {
                        var result = serverClinent.Item2.GetPerformanceCounterResult();

                        lock (sync)
                        {
                            stats.Add(new ServerPerformanceCounterStats(serverClinent.Item1, result));
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error(String.Format("Exception retrieving statistics from remote server name {0}.", serverClinent.Item1), ex);
                        failedRemoteServers.Add(serverClinent);
                    }


                });

                // try to reconstruct clients for monitoring servers
                SandboxDatabaseManagerServiceClient client;
                string remoteAddress = "";
                foreach (Tuple<string, SandboxDatabaseManagerServiceClient> friendlyFailedServers in failedRemoteServers)
                {
                    try
                    {
                        Log.InfoFormat("Trying to re-establish connection with monitored server: {0}", friendlyFailedServers.Item1);
                        remoteAddress = MonitoredServers.Instance.GetRemoteAddress(friendlyFailedServers.Item1);
                        client = new SandboxDatabaseManagerServiceClient("NetTcpBinding_ISandboxDatabaseManagerService", remoteAddress);
                        MyMonitoredServers.Remove(friendlyFailedServers);
                        MyMonitoredServers.Add(new Tuple<string, SandboxDatabaseManagerServiceClient>(friendlyFailedServers.Item1, client));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(String.Format("Failed to re-construct SandboxDatabaseManagerServiceClient for client FriendlyName: {0}, RemotAddress: {1}", friendlyFailedServers.Item1, remoteAddress), ex);
                    }
                }


                CurrentStats = stats;

                try
                {
                    Clients.All.updatePerformanceCounterStats(CurrentStats);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception while updating stats to clients.", ex);
                }


                _section.EnterWriteLock();
                try
                {

                    foreach (var serverStats in stats)
                    {
                        foreach (var counter in serverStats.ListOfCounterValues)
                        {

                            _statsToStore.Rows.Add(serverStats.ServerName, counter.CounterFriendlyName, counter.CounteValue, serverStats.StatsTime);
                        }

                    }

                }
                finally
                {
                    _section.ExitWriteLock();
                }

                if ((DateTime.Now - _lastRemoveOldCounterDataEach) > _removeOldCounterDataEach)
                {
                    _lastRemoveOldCounterDataEach = DateTime.Now;
                    Task.Run(() =>
                    {
                       
                        try
                        {
                            Database.DatabaseContext.RemoveOldDataFromDB();

                            _section.EnterWriteLock();
                            try
                            {
                                _countersDataMinDates = Database.DatabaseContext.GetCountersDataMinDates();
                            }
                            finally
                            {
                                _section.ExitWriteLock();
                            }
                            
                        }
                        catch (Exception ex) { Log.Error(ex); }
                    });
                }


                if ((DateTime.Now - _lastMaxStored) > _storeCounterDataEach)
                {
                    DataTable dtResults = new DataTable();
                    _section.EnterReadLock();
                    try
                    {

                        DataView dv = new DataView(_statsToStore);
                        dv.RowFilter = "CounterValueDate > '" + _lastMaxStored.ToString("o") + "'";
                        dtResults = dv.ToTable();
                    }
                    finally
                    {
                        _section.ExitReadLock();
                    }

                    

                    if (dtResults.Rows.Count > 0)
                    {

                        _lastMaxStored = (DateTime)dtResults.Compute("MAX(CounterValueDate)", null);

                        Task.Run(() =>
                        {
                           
                            try
                            {
                                Database.DatabaseContext.StoreCountersData(dtResults);
                            }
                            catch (Exception ex) { Log.Error(ex); }

                        });


                        _section.EnterWriteLock();
                        try
                        {

                            string removeOlderThan = _lastMaxStored.AddHours(-1).ToString("s");
                            foreach (var row in _statsToStore.Select("CounterValueDate <= '" + removeOlderThan + "'"))
                            {
                                _statsToStore.Rows.Remove(row);
                            }
                        }finally
                        {
                            _section.ExitWriteLock();
                        }

                        
                    }

                }




                await Task.Delay(10000);
            }
        }
        public class ServerPerformanceCounterStats
        {
            public class PerformanceCounterItem
            {
                public string CounterFriendlyName;
                public float CounteValue;
                public String DotNetFormatString;
                public String ChartYAxisSufix;
                public bool IsWarning;
                public string CounterFormattedValue
                {
                    get
                    {
                        if (!string.IsNullOrWhiteSpace(DotNetFormatString))
                            return String.Format(DotNetFormatString, CounteValue);
                        else
                            return CounteValue.ToString();
                    }
                }
            }

            public ServerPerformanceCounterStats() { }
            public ServerPerformanceCounterStats(string serverName, PerformanceCounterResults result)
            {
                this.StatsTime = DateTime.Now;

                this.ServerName = serverName;
                foreach (var counter in result.ResultList)
                {
                    ListOfCounterValues.Add(new PerformanceCounterItem() { CounteValue = counter.CounteValue, CounterFriendlyName = counter.CounterFriendlyName, DotNetFormatString = counter.DotNetFormatString, ChartYAxisSufix = counter.ChartYAxisSufix, IsWarning = counter.Warning });
                }

            }

            public string ServerName;
            public DateTime StatsTime;
            public string JsonDateTime
            {
                get
                {
                    return StatsTime.ToString("yyyy-MM-ddTHH:mm:ss.000");
                }
            }
            public List<PerformanceCounterItem> ListOfCounterValues = new List<PerformanceCounterItem>();

        }
        public static MonitoringBackgroundWorker Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        public static void Initialize()
        {
            var dummmy = Instance;
        }
        public DataTable GetCounters(string serverName, string counterName)
        {
            _section.EnterReadLock();
            try
            {

                DataView dv = new DataView(this._statsToStore, String.Format("ServerFriendlyName = '{0}' and CounterFriendlyName = '{1}'", serverName, counterName), "CounterValueDate asc", DataViewRowState.CurrentRows);
                return dv.ToTable();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                _section.ExitReadLock();
            }

            return null;

        }
        public DateTime GetCounterDataMinDate(string serverName, string counterName)
        {

            if (this._countersDataMinDates == null)
            {
                _section.EnterWriteLock();
                try
                {
                    if (_countersDataMinDates == null)
                        _countersDataMinDates = Database.DatabaseContext.GetCountersDataMinDates();
                }
                finally
                {
                    _section.ExitWriteLock();

                }
            }

            _section.EnterReadLock();
            try
            {

                var data = _countersDataMinDates.Rows.Cast<DataRow>().FirstOrDefault(row => String.Compare(row["ServerFriendlyName"].ToString(), serverName, true) == 0 && String.Compare(row["CounterFriendlyName"].ToString(), counterName, true) == 0);

                if (data == null)
                    return DateTime.Now;

                return (DateTime)data["MinCounterValueDate"];

            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                _section.ExitReadLock();

            }

            return DateTime.Now;
        }
        public List<ServerPerformanceCounterStats> CurrentStats
        {
            get
            {
                lock (sync)
                {
                    return currentStats;
                }

            }
            private set
            {
                lock (sync)
                {
                    currentStats = value;
                }
            }
        }


 

    }
}