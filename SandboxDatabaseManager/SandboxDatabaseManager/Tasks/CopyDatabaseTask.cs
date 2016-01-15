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
    public class CopyDatabaseTask: BaseBgTask
    {
        private string _sourceDatabaseServer;
        private string _sourceDatabaseName;
        
        private string _targetDatabaseServer;
        private string _targetDatabaseName;

        private string _databaseComment;
        private string _databaseOwner;
        private bool _changeRecoveryToSimple;

        private SqlCommand _commandBackup;

        CancellationTokenSource _cancelToken = new CancellationTokenSource();

        public CopyDatabaseTask(string sourceDatabaseServer, string sourceDatabaseName, string targetDatabaseServer, string targetDatabaseName, string databaseComment, bool changeRecoveryToSimple, string databaseOwner)
        {
            _sourceDatabaseServer = sourceDatabaseServer;
            _sourceDatabaseName = sourceDatabaseName;
            _targetDatabaseServer = targetDatabaseServer;
            _targetDatabaseName = targetDatabaseName;
            _databaseComment = databaseComment;
            _databaseOwner = databaseOwner;
            _changeRecoveryToSimple = changeRecoveryToSimple;

            Owner = databaseOwner;
            SupportsAbort = true;
            Name = string.Format("Copy database {0}.{1} to {2}.{3}", sourceDatabaseServer, sourceDatabaseName, targetDatabaseServer, targetDatabaseName);
        }

        public override void Start()
        {
            try
            {
                Status = TaskStatus.Running;

                var token = _cancelToken.Token;

                Task.Run(() =>
                    {
                        try
                        {
                            var targetServer = DatabaseServers.Instance.ItemsList.First(server => server.Name == _targetDatabaseServer);
                            string outputFileName;

                            using (SqlConnection conn = new SqlConnection(DatabaseServers.Instance.ItemsList.First(server => server.Name == _sourceDatabaseServer).ConnectionString))
                            {
                                _commandBackup = conn.CreateCommand();
                                _commandBackup.CommandTimeout = 66000;
                                _commandBackup.CommandType = CommandType.StoredProcedure;
                                _commandBackup.CommandText = "dbo.BackupDatabase";
                                _commandBackup.Parameters.Add(new SqlParameter("@DatabaseName", _sourceDatabaseName));
                                _commandBackup.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                _commandBackup.Parameters.Add(new SqlParameter("@TargetBackupDestinationPath", targetServer.CopyDatabaseNetworkSharePath));
                                _commandBackup.Parameters.Add(new SqlParameter("@Guid", ID));
                                SqlParameter sqlOutputFileName = new SqlParameter("@BackupFileName", SqlDbType.NVarChar, -1);
                                sqlOutputFileName.Direction = ParameterDirection.Output;
                                _commandBackup.Parameters.Add(sqlOutputFileName);

                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                _commandBackup.ExecuteNonQuery();
                                conn.Close();
                                _commandBackup = null;

                                outputFileName = sqlOutputFileName.Value.ToString();
                            }

                            if (Status == TaskStatus.Failed)
                                return;

                            token.ThrowIfCancellationRequested();

                            AppendOutputText(Environment.NewLine);

                            using (SqlConnection conn = new SqlConnection(targetServer.ConnectionString))
                            {
                                var commandRestore = conn.CreateCommand();
                                commandRestore.CommandTimeout = 66000;
                                commandRestore.CommandType = CommandType.StoredProcedure;
                                commandRestore.CommandText = "dbo.RestoreDatabase";
                                commandRestore.Parameters.Add(new SqlParameter("@DatabaseName", _targetDatabaseName));
                                commandRestore.Parameters.Add(new SqlParameter("@FileCollection", String.Format("<FileList><FilePath>{0}</FilePath></FileList>", outputFileName)));
                                commandRestore.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                commandRestore.Parameters.Add(new SqlParameter("@AdHocBackupRestore", true));
                                commandRestore.Parameters.Add(new SqlParameter("@ChangeRecoveryToSimple", _changeRecoveryToSimple));
                                commandRestore.Parameters.Add(new SqlParameter("@OriginatingServer", _sourceDatabaseServer));
                                commandRestore.Parameters.Add(new SqlParameter("@OriginatingDatabaseName", _sourceDatabaseName));
                                commandRestore.Parameters.Add(new SqlParameter("@DatabaseComment", String.IsNullOrWhiteSpace(_databaseComment) ? DBNull.Value : (object)_databaseComment));
                                commandRestore.Parameters.Add(new SqlParameter("@BackupTypeDescription", "DATABASE"));
                                commandRestore.Parameters.Add(new SqlParameter("@RestoreWithRecovery", true));
                                
                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                commandRestore.ExecuteNonQuery();
                                conn.Close();
                            }



                            Status = TaskStatus.Succeeded;
                        }
                        catch (TaskCanceledException)
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
            }
            catch (Exception ex)
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
                _cancelToken.Cancel();
                if (_commandBackup != null)
                {
                    _commandBackup.Cancel();
                }
            }
            catch { };
        }


        

    }
}
