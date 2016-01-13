using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Tasks
{
    public class TaskHist : IBgTask
    {
        
        public void Start()
        {
            
        }

        public void RequestAbort()
        {

        }

        public void PopulateParametersForHistory(SqlCommand command)
        {
            
        }

        public string ID { get; set; }
        public string OutputText { get; set; }
        public string Owner { get; set; }
        public TaskStatus Status { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public object Result { get; set; }

        public object RedirectOnlyResult { get; set; }

        public bool DiscardedFromStatsReporting { get; set; }

        public bool SupportsAbort
        {
            get
            {
                return false;
            }
        }

        public string RedirectToController { get; set; }

        public string RedirectToAction { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
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

        


       

        public event TaskStatusChanged OnTaskStatusChanged;
    }
}