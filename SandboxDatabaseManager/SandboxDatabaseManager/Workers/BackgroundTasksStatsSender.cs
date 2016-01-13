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

namespace SandboxDatabaseManager.Worker
{
    public class BackgroundTasksStatsSender
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Lazy<BackgroundTasksStatsSender> _instance = new Lazy<BackgroundTasksStatsSender>(() => new BackgroundTasksStatsSender(GlobalHost.ConnectionManager.GetHubContext<BackgroundTasksHub>().Clients));
        public List<Tuple<string, SandboxDatabaseManagerServiceClient>> MyMonitoredServers;
        private object sync = new object();

        private IHubConnectionContext<dynamic> Clients
        {
            get;
            set;
        }
        private BackgroundTasksStatsSender(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;

        }
        public static BackgroundTasksStatsSender Instance
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

        private Dictionary<string, List<BackgroundTasksStat>> _stats = new Dictionary<string, List<BackgroundTasksStat>>();

        public Dictionary<string, List<BackgroundTasksStat>> Stats
        {
            get
            {
                return _stats;
            }
            set
            {
                _stats = value;

            }
        }

        private Dictionary<string, List<string>> _usersToConnections = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>>  UsersToConnections
        {
            get { return _usersToConnections; }
            set { _usersToConnections = value; }
        }

        public void TaskStatusChanged(string Owner, string taskId, SandboxDatabaseManager.Tasks.TaskStatus status)
        {

            if(!Stats.ContainsKey(Owner))
            {
                lock(Stats)
                {
                    // now check once more this time in locked state.
                    if (!Stats.ContainsKey(Owner))
                    {
                        Stats[Owner] = new List<BackgroundTasksStat>();
                    }
                
                }
            }

            var tasks = Stats[Owner];

            List<StatItem> resultToSend;

            lock (tasks)
            {
                var taskRetrieved =  tasks.FirstOrDefault(item => item.TaskID == taskId);
                if(taskRetrieved != null)
                {
                    taskRetrieved.Status = status;
                }
                else
                {
                    tasks.Add(new BackgroundTasksStat(taskId, status));
                }

                resultToSend = tasks.GroupBy(item => item.Status).Select(item => new StatItem() { Status = item.Key.ToString(), Count = item.Count() }).ToList();
            }

            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString()))
                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString(), Count = 0 });

            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString()))
                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString(), Count = 0 });

            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString()))
                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString(), Count = 0 });

            List<string> userConnections;
            
            
            if(UsersToConnections.TryGetValue(Owner, out userConnections))
            {

                lock (userConnections)
                {
                    List<string> problematicConnectionsToRemove = new List<string>();

                    foreach (string connection in userConnections)
                    {
                        try
                        {
                            Clients.Client(connection).updateBackgroundTaskStats(resultToSend);
                        }
                        catch (Exception ex)
                        {
                            problematicConnectionsToRemove.Add(connection);
                            Log.Error("Exception while updating background task stats to client.", ex);
                        }
                    }

                    foreach (var toRemove in problematicConnectionsToRemove)
                    {
                        userConnections.Remove(toRemove);
                    }
                }
            }

        }

        public void DiscardTasksFromReporting(string Owner, List<string> taskIDs)
        {
            Task.Run(() => 
            {

                if (taskIDs == null || taskIDs.Count == 0)
                    return;

                List<BackgroundTasksStat> listToInvestigate;



                if (Stats.TryGetValue(Owner, out listToInvestigate))
                {
                    int beforeCount = listToInvestigate.Count;

                    lock (listToInvestigate)
                    {
                        listToInvestigate.RemoveAll(item => taskIDs.Contains(item.TaskID));
                    }



                    if (beforeCount != listToInvestigate.Count)
                    {
                        List<string> userConnections;
                        if (UsersToConnections.TryGetValue(Owner, out userConnections))
                        {

                            var tasks = Stats[Owner];

                            List<StatItem> resultToSend;

                            lock (tasks)
                            {
                                resultToSend = tasks.GroupBy(item => item.Status).Select(item => new StatItem() { Status = item.Key.ToString(), Count = item.Count() }).ToList();
                            }

                            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString()))
                                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString(), Count = 0 });

                            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString()))
                                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString(), Count = 0 });

                            if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString()))
                                resultToSend.Add(new StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString(), Count = 0 });


                            lock (userConnections)
                            {
                                List<string> problematicConnectionsToRemove = new List<string>();

                                foreach (string connection in userConnections)
                                {
                                    try
                                    {
                                        Clients.Client(connection).updateBackgroundTaskStats(resultToSend);
                                    }
                                    catch (Exception ex)
                                    {
                                        problematicConnectionsToRemove.Add(connection);
                                        Log.Error("Exception while updating background task stats to client.", ex);
                                    }
                                }

                                foreach (var toRemove in problematicConnectionsToRemove)
                                {
                                    userConnections.Remove(toRemove);
                                }
                            }
                        }
                    }

                }
            });

        }


        public class BackgroundTasksStat
        {
            public string TaskID { get; set; }
            public SandboxDatabaseManager.Tasks.TaskStatus Status { get; set; }

            public BackgroundTasksStat() { }
            public BackgroundTasksStat(string taskID, SandboxDatabaseManager.Tasks.TaskStatus status)
            {
                TaskID = taskID;
                Status = status;
            }

        }

        public class StatItem
        {
            public string Status { get; set; }
            public int Count { get; set; }
        }

    }
}