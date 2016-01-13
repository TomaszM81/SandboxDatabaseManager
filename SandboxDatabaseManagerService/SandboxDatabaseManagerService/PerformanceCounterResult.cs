using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandboxDatabaseManagerService
{
    public class PerformanceCounterResults
    {
        public class PerformanceCounterResult
        {
            public String CounterFriendlyName;
            public float CounteValue;
            public String DotNetFormatString;
            public String ChartYAxisSufix;
            public bool Warning;
        }

        public List<PerformanceCounterResult> ResultList = new List<PerformanceCounterResult>();
    }


}
