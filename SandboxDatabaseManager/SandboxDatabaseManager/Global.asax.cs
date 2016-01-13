using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Worker;
using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "Web.config", Watch = true)]
namespace SandboxDatabaseManager
{
    
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger("Sandb" + "ox Databas" + "e Ma" +"nager");

        protected void Application_Start()
        {

             log4net.Config.XmlConfigurator.Configure(new FileInfo(Server.MapPath("~/Web.config")));

             AreaRegistration.RegisterAllAreas();
             FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
             RouteConfig.RegisterRoutes(RouteTable.Routes);
             BundleConfig.RegisterBundles(BundleTable.Bundles);
             GlobalFilters.Filters.Add(new HandleErrorAttribute());

             MonitoringBackgroundWorker.Initialize();
             UserPermissions.Initialize();
             GarbageFileCollector.Initialize();
             TaskContainer.Initialize();
             DatabaseServerSettings.Initialize();

        }

        public MvcApplication()
        {
            PreRequestHandlerExecute += MvcApplication_BeginRequest;
        }

        void MvcApplication_BeginRequest(object sender, EventArgs e)
        {
            if (!UserPermissions.Instance.HasSandboxDatabaseManagerAccess(Context.Request.LogonUserIdentity.Name) && !Context.Request.Url.AbsoluteUri.ToLower().Contains("/nopowerhere.html"))
            {
                Log.ErrorFormat("No Sandbox Database Manager Web Application access for user:{0}", Context.Request.LogonUserIdentity.Name);
                Context.Response.Redirect("~/nopowerhere.html");
            }

        }
    }
}

