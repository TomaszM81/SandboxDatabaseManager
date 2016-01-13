using SandboxDatabaseManager.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SandboxDatabaseManager.Tasks
{
    public class DisableChangeTrackingTask : BaseBgTask
    {
        private string _databaseServer;
        private string _databaseName;
        private string _databaseOwner;

        public DisableChangeTrackingTask(string databaseServer, string databaseName, string databaseOwner)
        {
            _databaseServer = databaseServer;
            _databaseName = databaseName;
            _databaseOwner = databaseOwner;

            Owner = databaseOwner;
            SupportsAbort = false;

            Name = string.Format("Disable Tracking Changes on server: {0}, database: {1}", databaseServer, databaseName);
        }

        public override void Start()
        {
            try
            {
                Status = TaskStatus.Running;

                Task.Run(() =>
                    {
                        try 
                        {
                            using (SqlConnection conn = new SqlConnection(DatabaseServers.Instance.ItemsList.First(server => server.Name == _databaseServer).ConnectionString))
                            {
                                var _command = conn.CreateCommand();
                                _command.CommandTimeout = 360;
                                _command.CommandType = CommandType.StoredProcedure;
                                _command.CommandText = "dbo.ChangeTrackingDisable";
                                _command.Parameters.Add(new SqlParameter("@DatabaseName", _databaseName));
                                _command.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                
                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                _command.ExecuteNonQuery();
                                conn.Close();

                            }

                            Status = TaskStatus.Succeeded;
                        }
                        catch(TaskCanceledException)
                        {
                            Status = TaskStatus.Aborted;
                        }
                        catch (Exception ex)
                        {
                            AppendOutputText(ex.Message);
                            Status = TaskStatus.Failed;
                            Log.Error("Failed to disable change tracking on database.", ex);
                        }
                    });
            }catch(Exception ex)
            {
                Log.Error("Unable to start task.", ex);
                throw;
            }
        }

        private void connInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            bool founderrors = false;
            foreach (SqlError error in e.Errors)
            {
                if (error.Class > 10)
                {
                    AppendOutputText("Error !!! " + Environment.NewLine); ;
                    AppendOutputText(error.Message + Environment.NewLine);
                    founderrors = true;
                    Status = TaskStatus.Failed;
                    Log.Error("Error while executing SQL command:" + error.Message);
                }
            }

            if (!founderrors)
            {
                AppendOutputText(e.Message + Environment.NewLine);
            }
        }
        

    }
}