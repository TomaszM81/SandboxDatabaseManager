using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandboxDatabaseManager.Tasks
{
    public interface IBgTask
    {
        event TaskStatusChanged OnTaskStatusChanged;

        string ID
        {
            get;
        }

        string Owner
        {
            get;
        }

        TaskStatus Status
        {
            get;
        }
        string OutputText
        {
            get;
        }
        string Name
        {
            get;
        }
        string Description
        {
            get;
        }
        object Result
        {
            get;
        }
        
        object RedirectOnlyResult
        {
            get;
        }

        bool SupportsAbort
        {
            get;
        }

        string RedirectToController
        {
            get;
        }
        string RedirectToAction
        {
            get;
        }

        DateTime StartDate
        {
            get;
        }

        DateTime EndDate
        {
            get;
        }

        //"{0:00}:{1:00}:{2:00} sec"
        // String.Format("{0:00}:{1:00}:{2:00} sec", duration.TotalHours, duration.Minutes, duration.Seconds)
        TimeSpan Duration
        {
            get;
        }

        string DurationString
        {
            get;
        }

        bool Finished
        {
            get;
        }

        bool DiscardedFromStatsReporting
        {
            get;
            set;
        }

        void RequestAbort();
        void Start();


        void PopulateParametersForHistory(SqlCommand command);
    }
}
