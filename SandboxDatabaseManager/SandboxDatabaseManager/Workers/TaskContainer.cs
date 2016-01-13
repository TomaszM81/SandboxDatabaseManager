using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SandboxDatabaseManager.Tasks;
using SandboxDatabaseManager.Database;

namespace SandboxDatabaseManager.Worker
{
    public class TaskContainer
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Lazy<TaskContainer> _instance = new Lazy<TaskContainer>(() => new TaskContainer());
        private Task backgroundTask;
        private ReaderWriterLockSlim _section = new ReaderWriterLockSlim();
        private TaskContainer()
        {
            backgroundTask = new Task(RemoveOldTasks, TaskCreationOptions.LongRunning);
            backgroundTask.Start();
            _taskList = DatabaseContext.GetTaskHistoryFromDB();
        }

        public static void Initialize()
        {
            var dummmy = Instance;
        }

        async void RemoveOldTasks()
        {

            while (true)
            {
                try
                {


                    await Task.Delay(new TimeSpan(30, 0, 0));

                    _section.EnterWriteLock();
                    try
                    {
                        foreach (var groups in _taskList.Where(task => task.Finished).GroupBy(task => task.Owner).Select( item => new { item.Key, tasks = item.OrderBy(task => task.EndDate).ToList() }))
                        {
                            for (int i = 15; i < groups.tasks.Count; i++)
                                _taskList.Remove(groups.tasks[i]);
                        }
                    }
                    finally
                    {
                        _section.ExitWriteLock();
                    }

                   

                }
                catch (Exception ex)
                {
                    Log.Error("Exception in RemoveOldTasks.", ex);
                }
            }

        }

        private List<IBgTask> _taskList = new List<IBgTask>();
        public static TaskContainer Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public List<IBgTask> GetRecentTop15TasksForUser(string owner, string status = "")
        {
            _section.EnterReadLock();
            try
            {
                return _taskList.Where(task => String.Compare(task.Owner, owner, true) == 0 && ((task.Status.ToString() == status && !task.DiscardedFromStatsReporting) || status == "")).OrderByDescending(task => task.StartDate).ToList();
            }
            finally
            {
                _section.ExitReadLock();
            }
        }

        public List<IBgTask> GetTasksForUser(string owner)
        {
            _section.EnterReadLock();
            try
            {
                return _taskList.Where(task => String.Compare(task.Owner, owner, true) == 0).OrderByDescending(task => task.StartDate).Take(10).ToList();
            }
            finally
            {
                _section.ExitReadLock();
            }
        }

        public void AddTask(IBgTask taskToAdd)
        {
            _section.EnterWriteLock();
            try
            {
                _taskList.Add(taskToAdd);
            }
            finally
            {
                _section.ExitWriteLock();
            }

           

        }

        public void RemoveTask(IBgTask task)
        {
            _section.EnterWriteLock();
            try
            {
                BackgroundTasksStatsSender.Instance.DiscardTasksFromReporting(task.Owner, new List<string>() { task.ID });
                DatabaseContext.RemoveTaskHistory(task);
                _taskList.Remove(task);
            }
            finally
            {
                _section.ExitWriteLock();
            }
        }

        public IBgTask GetTask(string id)
        {
            _section.EnterReadLock();
            try
            {
                return _taskList.FirstOrDefault(task => String.Compare(task.ID, id, true) == 0);
            }
            finally
            {
                _section.ExitReadLock();
            }


        }
    }
}