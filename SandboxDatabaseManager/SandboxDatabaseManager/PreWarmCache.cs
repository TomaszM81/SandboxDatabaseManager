
using SandboxDatabaseManager.Worker;
using System.IO;


namespace SandboxDatabaseManager
{
    public class PreWarmCache : System.Web.Hosting.IProcessHostPreloadClient
    {

        public void Preload(string[] parameters)
        {

            MonitoringBackgroundWorker.Initialize();
            UserPermissions.Initialize();
            GarbageFileCollector.Initialize();
            TaskContainer.Initialize();

        }

    }
}