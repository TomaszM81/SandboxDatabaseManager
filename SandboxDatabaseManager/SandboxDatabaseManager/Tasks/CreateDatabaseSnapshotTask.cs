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
    public class CreateDatabaseSnapshotTask: BaseBgTask
    {
        private string _sourceDatabaseServer;
        private string _sourceDatabaseName;
        
        private string _databaseSnapshotName;
        private string _databaseOwner;

        private SqlCommand _commandCreateSnapshot;

        public CreateDatabaseSnapshotTask(string sourceDatabaseServer, string sourceDatabaseName, string databaseSnapshotName, string databaseOwner)
        {
            _sourceDatabaseServer = sourceDatabaseServer;
            _sourceDatabaseName = sourceDatabaseName;
            _databaseSnapshotName = databaseSnapshotName;
            _databaseOwner = databaseOwner;

            Owner = databaseOwner;
            SupportsAbort = false;
            Name = string.Format("Create {0} snapshot from {1}.{2} database", databaseSnapshotName, sourceDatabaseServer, sourceDatabaseName);
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
                            using (SqlConnection conn = new SqlConnection(DatabaseServers.Instance.ItemsList.First(server => server.Name == _sourceDatabaseServer).ConnectionString))
                            {
                                _commandCreateSnapshot = conn.CreateCommand();
                                _commandCreateSnapshot.CommandTimeout = 66000;
                                _commandCreateSnapshot.CommandType = CommandType.StoredProcedure;
                                _commandCreateSnapshot.CommandText = "dbo.CreateDatabaseSnapshot";
                                _commandCreateSnapshot.Parameters.Add(new SqlParameter("@SourceDatabaseName", _sourceDatabaseName));
                                _commandCreateSnapshot.Parameters.Add(new SqlParameter("@DatabaseSnapshotName", _databaseSnapshotName));
                                _commandCreateSnapshot.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                
                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                _commandCreateSnapshot.ExecuteNonQuery();
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
                            Log.Error("Failed to Copy database.", ex);
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