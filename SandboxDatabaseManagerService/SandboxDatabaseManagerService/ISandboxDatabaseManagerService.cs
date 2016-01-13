using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SandboxDatabaseManagerService
{
    [ServiceContract]
    public interface ISandboxDatabaseManagerService
    {
        [OperationContract]
        PerformanceCounterResults GetPerformanceCounterResult();
    }
}
