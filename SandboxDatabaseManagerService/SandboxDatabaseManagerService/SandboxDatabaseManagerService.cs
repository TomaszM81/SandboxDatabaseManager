using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Configuration;
using log4net.Config;

namespace SandboxDatabaseManagerService
{
    public partial class SandboxDatabaseManagerService : ServiceBase
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ServiceHost m_svcHost = null;  
        public SandboxDatabaseManagerService()
        {
            InitializeComponent();
        }
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            XmlConfigurator.Configure();

            try
            {
                if (m_svcHost != null) m_svcHost.Close();

                string strAdrTCP = String.Format("net.tcp://localhost:{0}/SandboxDatabaseManagerService", ConfigurationManager.AppSettings["TCP_Port_Num"]);

                Log.Info(String.Format("Configured address: {0}", strAdrTCP));

                Uri[] adrbase = { new Uri(strAdrTCP) };
                m_svcHost = new ServiceHost(typeof(SandboxDatabaseService), adrbase);

                ServiceMetadataBehavior mBehave = new ServiceMetadataBehavior();
                m_svcHost.Description.Behaviors.Add(mBehave);

                NetTcpBinding tcpb = new NetTcpBinding(SecurityMode.None);
                m_svcHost.AddServiceEndpoint(typeof(ISandboxDatabaseManagerService), tcpb, strAdrTCP);
                m_svcHost.AddServiceEndpoint(typeof(IMetadataExchange),
                MetadataExchangeBindings.CreateMexTcpBinding(), "mex");

                m_svcHost.Open();

                PerformanceCounters.Initiallize();
            }
            catch(Exception ex)
            {
                Log.Error("Failed to start the service due to exception.", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            if (m_svcHost != null)
            {
                m_svcHost.Close();
                m_svcHost = null;
            }
        }

        //public static int Main(String[] args)
        //{
        //    (new SandboxDatabaseManagerService()).OnStart(args); // allows easy debugging of OnStart()
        //    ServiceBase.Run(new SandboxDatabaseManagerService());
        //    return 1;
        //}
    }
}
