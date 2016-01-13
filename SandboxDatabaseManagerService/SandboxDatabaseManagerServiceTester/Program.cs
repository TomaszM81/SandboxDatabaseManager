using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace CLRFunctionPerformanceCounter
{
    class Program
    {
        static void Main(string[] args)
        {

            SandboxDatabaseManagerService.SandboxDatabaseManagerServiceClient client = new SandboxDatabaseManagerService.SandboxDatabaseManagerServiceClient("NetTcpBinding_ISandboxDatabaseManagerService", "net.tcp://localhost:9087/SandboxDatabaseManagerService");

          

            while (true)
            {


                var result = client.GetPerformanceCounterResult();

                foreach (var item in result.ResultList)
                {
                    Console.WriteLine(item.CounterFriendlyName + " " + item.CounterFormattedValue);
                }
                Console.WriteLine("------------------------------------------------------------");
                Thread.Sleep(4000);

            }
        }
    }
}
