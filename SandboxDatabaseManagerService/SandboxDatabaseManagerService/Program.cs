using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SandboxDatabaseManagerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                SandboxDatabaseManagerService service1 = new SandboxDatabaseManagerService();
                service1.TestStartupAndStop(null);
            }
            else
            {


                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new SandboxDatabaseManagerService() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
