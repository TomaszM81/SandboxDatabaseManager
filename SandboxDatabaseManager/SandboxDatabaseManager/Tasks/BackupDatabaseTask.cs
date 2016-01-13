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
    public class BackupDatabaseTask: BaseBgTask
    {
        private string _databaseServer;
        private string _databaseName;
        private string _databaseOwner;
        private string _backupComment;
        private string _targetBackupFileLocation;

        private SqlCommand _commandBackup;

        public BackupDatabaseTask(string databaseServer, string databaseName, string databaseOwner, string backupComment, string targetBackupFileLocation)
        {
            _databaseServer = databaseServer;
            _databaseName = databaseName;
            _databaseOwner = databaseOwner;
            _backupComment = backupComment;
            _targetBackupFileLocation = targetBackupFileLocation;

            Owner = databaseOwner;
            SupportsAbort = true;
            Name = string.Format("Backup {1}.{0} database to location: {2}.", databaseName, databaseServer, targetBackupFileLocation);
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
                                _commandBackup = conn.CreateCommand();
                                _commandBackup.CommandTimeout = 66000;
                                _commandBackup.CommandType = CommandType.StoredProcedure;
                                _commandBackup.CommandText = "dbo.BackupDatabase";
                                _commandBackup.Parameters.Add(new SqlParameter("@DatabaseName", _databaseName));
                                _commandBackup.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                _commandBackup.Parameters.Add(new SqlParameter("@BackupComment", _backupComment));
                                _commandBackup.Parameters.Add(new SqlParameter("@TargetBackupFilePath", _targetBackupFileLocation));
                                SqlParameter sqlOutputFileName = new SqlParameter("@BackupFileName", SqlDbType.NVarChar, -1);
                                sqlOutputFileName.Direction = ParameterDirection.Output;
                                _commandBackup.Parameters.Add(sqlOutputFileName);
                              

                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                _commandBackup.ExecuteNonQuery();
                            }

                            if (Status == TaskStatus.Failed)
                                return;

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
                            Log.Error("Failed to backup database.", ex);
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

        public override void RequestAbort()
        {
            try
            {
                if (_commandBackup != null)
                {
                    _commandBackup.Cancel();
                }
            }
            catch { };
        }
        

    }
}