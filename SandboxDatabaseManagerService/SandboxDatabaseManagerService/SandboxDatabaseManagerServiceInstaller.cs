using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace SandboxDatabaseManagerService
{
    [RunInstaller(true)]
    public partial class SandboxDatabaseManagerServiceInstaller : System.Configuration.Install.Installer
    {
        public SandboxDatabaseManagerServiceInstaller()
        {
            //InitializeComponent();
            serviceProcessInstaller1 = new ServiceProcessInstaller();
            serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            serviceInstaller1 = new ServiceInstaller();
            serviceInstaller1.ServiceName = "SandboxDatabaseManagerService";
            serviceInstaller1.DisplayName = "SandboxDatabaseManagerService";
            serviceInstaller1.Description = "SandboxDatabaseManager application helper service";
            serviceInstaller1.StartType = ServiceStartMode.Automatic;
            Installers.Add(serviceProcessInstaller1);
            Installers.Add(serviceInstaller1);

        }
    }
}
