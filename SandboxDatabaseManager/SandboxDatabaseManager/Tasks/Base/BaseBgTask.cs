using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SandboxDatabaseManager.Tasks
{
    public abstract class BaseBgTask : IBgTask
    {
        protected Task _task;
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private StringBuilder _outputText = new StringBuilder();
        private object _sync = new object();
        public BaseBgTask()
        {
            ID = Guid.NewGuid().ToString().ToUpper();
            this.OnTaskStatusChanged += BackgroundTasksStatsSender.Instance.TaskStatusChanged;

        }
        public string ID
        {
            get;
            private set;
        }
        protected void AppendOutputText(string text)
        {
            lock (_sync)
            {
                _outputText.Append(text);
            }

        }

        public string OutputText
        {
            get
            {
                lock (_sync)
                {
                    return _outputText.ToString();
                }
            }
        }

        public string Owner
        {
            get;
            protected set;
        }


        public bool SupportsAbort
        {
            get;
            protected set;
        }

        public object Result
        {
            get;
            protected set;
        }
        /// <summary>
        /// This result is only used to pass aditional parameters to the redirect controller/action. It shold not be used to store meaningfull results that can be used to present data after the task has completed and later on.
        /// </summary>
        public object RedirectOnlyResult
        {
            get;
            protected set;
        }

        private TaskStatus _status = TaskStatus.NotStarted;

        public event TaskStatusChanged OnTaskStatusChanged;

        public TaskStatus Status
        {
            get { return _status; }
            protected set 
            {
                if (value > TaskStatus.NotStarted && StartDate == default(DateTime))
                    StartDate = DateTime.Now;

                if (value > TaskStatus.Running && EndDate == default(DateTime))
                {
                    Task.Run(() => DatabaseContext.InsertTaskHistory(this));
                    EndDate = DateTime.Now;
                }
                    

                if (value > _status)
                {
                    DiscardedFromStatsReporting = false;
                    _status = value;
                    FireOnTaskStatusChanged( Owner, ID, value);
                }
            }

        }

        public string RedirectToController
        {
            get;
            protected set;
        }
        public string RedirectToAction
        {
            get;
            protected set;
        }

        public string Name
        {
            get;
            protected set;
        }

        public string Description
        {
            get;
            protected set;
        }
        public DateTime StartDate
        {
            get;
            protected set;
        }

        public DateTime EndDate
        {
            get;
            protected set;
        }

        //"{0:00}:{1:00}:{2:00} sec"
        // String.Format("{0:00}:{1:00}:{2:00} sec", duration.TotalHours, duration.Minutes, duration.Seconds)
        public TimeSpan Duration
        {
            get
            {
                if (Status == TaskStatus.NotStarted)
                    return TimeSpan.Zero;

                if (Status > TaskStatus.Running)
                    return EndDate - StartDate;
                else
                    return DateTime.Now - StartDate;

            }
        }

        public string DurationString
        {
           get
           {
               var duration = Duration;
               return String.Format("{0:00}:{1:00}:{2:00} sec", duration.TotalHours, duration.Minutes, duration.Seconds);
           }
        }

        public bool Finished
        {
            get
            {
                return Status > TaskStatus.Running;
            }
        }

        public bool DiscardedFromStatsReporting
        {
            get;
            set;
        }

        public abstract void Start();
        public virtual void RequestAbort() { }

        protected void FireOnTaskStatusChanged(string Owner, string taskId, TaskStatus status)
        {
            Delegate[] delegates = OnTaskStatusChanged.GetInvocationList();
            foreach (Delegate d in delegates)
            {
                try
                {
                    TaskStatusChanged ev = (TaskStatusChanged)d;
                    ev.BeginInvoke(Owner, taskId, status, null, null);
                }catch(Exception ex)
                {
                    Log.Error("Failed to rise TaskStatusChanged event.", ex);
                }
            }
        }

        public virtual void PopulateParametersForHistory(SqlCommand command)
        {
            command.Parameters.AddWithValue("@ID", ID);
            command.Parameters.AddWithValue("@Name", Name);
            command.Parameters.AddWithValue("@OutputText", OutputText);
            command.Parameters.AddWithValue("@Owner", Owner);
            command.Parameters.AddWithValue("@Status", Status);
            command.Parameters.AddWithValue("@RedirectToController", RedirectToController);
            command.Parameters.AddWithValue("@RedirectToAction", RedirectToAction);
            command.Parameters.AddWithValue("@Description", Description);
            command.Parameters.AddWithValue("@StartDate", StartDate);
            command.Parameters.AddWithValue("@EndDate", StartDate);
        }

    }
}