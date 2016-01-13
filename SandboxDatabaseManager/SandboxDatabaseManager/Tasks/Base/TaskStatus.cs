using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Tasks
{
    public enum TaskStatus
    {
        NotStarted = 0,
        Running,
        Succeeded,
        Failed,
        Aborted
        
    }
}