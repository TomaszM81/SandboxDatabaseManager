using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandboxDatabaseManagerService
{
    public class SandboxDatabaseService : ISandboxDatabaseManagerService
    {
        public PerformanceCounterResults GetPerformanceCounterResult()
        {
            return PerformanceCounters.GetPerformanceCounters();
        }
    }
}
