using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SandboxDatabaseManager.Worker.SignalR
{
    [HubName("backgroundTasksHub")]
    public class BackgroundTasksHub : Hub
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BackgroundTasksStatsSender instance;

        public BackgroundTasksHub() : this(BackgroundTasksStatsSender.Instance) { }

        public BackgroundTasksHub(BackgroundTasksStatsSender instance)
        {
            this.instance = instance;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            try
            {
                string Owner = Context.Request.User.Identity.Name;


                var UsersToConnections = instance.UsersToConnections;
                if (UsersToConnections.ContainsKey(Owner))
                {
                    var userConnections = UsersToConnections[Owner];

                    lock (userConnections)
                    {
                        if (userConnections.Contains(Context.ConnectionId))
                        {
                            userConnections.Remove(Context.ConnectionId);
                        }

                    }
                }
            }
            catch(Exception ex)
            {
               // Log.Error(ex);
            }


            return base.OnDisconnected(stopCalled);
        }

        public IEnumerable<BackgroundTasksStatsSender.StatItem> GetBackgroundStats()
        {
            try
            {
                string Owner = Context.Request.User.Identity.Name;

                var Stats = instance.Stats;
                var UsersToConnections = instance.UsersToConnections;

                if (!Stats.ContainsKey(Owner))
                {
                    lock (Stats)
                    {
                        // now check once more this time in locked state.
                        if (!Stats.ContainsKey(Owner))
                        {
                            Stats[Owner] = new List<BackgroundTasksStatsSender.BackgroundTasksStat>();
                        }

                    }
                }

                var tasks = Stats[Owner];
                List<BackgroundTasksStatsSender.StatItem> resultToSend;
                lock (tasks)
                {
                    resultToSend = tasks.GroupBy(item => item.Status).Select(item => new BackgroundTasksStatsSender.StatItem() { Status = item.Key.ToString(), Count = item.Count() }).ToList();
                }

                if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString()))
                    resultToSend.Add(new BackgroundTasksStatsSender.StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Running.ToString(), Count = 0 });

                if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString()))
                    resultToSend.Add(new BackgroundTasksStatsSender.StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Failed.ToString(), Count = 0 });

                if (!resultToSend.Any(item => item.Status == SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString()))
                    resultToSend.Add(new BackgroundTasksStatsSender.StatItem() { Status = SandboxDatabaseManager.Tasks.TaskStatus.Succeeded.ToString(), Count = 0 });




                if (!UsersToConnections.ContainsKey(Owner))
                {
                    lock (UsersToConnections)
                    {
                        // now check once more this time in locked state.
                        if (!UsersToConnections.ContainsKey(Owner))
                        {
                            UsersToConnections[Owner] = new List<string>();
                        }

                    }
                }

                var userConnections = UsersToConnections[Owner];

                lock (userConnections)
                {
                    if (!userConnections.Contains(Context.ConnectionId))
                    {
                        userConnections.Add(Context.ConnectionId);
                    }

                }

                return resultToSend;
            }
            catch(Exception ex)
            {
                Log.Error(ex);

            }

            return null;
        }




    }
}