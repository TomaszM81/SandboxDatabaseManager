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
    public class RestoreDatabaseTask : BaseBgTask
    {
        private string _databaseServer;
        private string _databaseName;
        private string _sourceDatabaseServer;
        private string _sourceDatabaseName;
        private string _databaseOwner;
        private string _databaseComment;
        private string _sourceBackupFileXMLCollection;
        private int _positinInFileCollection;
        private bool _changeRecoveryToSimple;
        private string _backupType;
        private bool _restoreWithRecovery;

        private SqlCommand _commandRestore;

        public RestoreDatabaseTask(string databaseServer, string databaseName, string databaseOwner, string sourceDatabaseServer, string sourceDatabaseName, string databaseComment, string sourceBackupFileXMLCollection, bool changeRecoveryToSimple, int positinInFileCollection, string backupType, bool restoreWithRecovery)
        {
            _databaseServer = databaseServer;
            _databaseName = databaseName;
            _databaseOwner = databaseOwner;
            _databaseComment = databaseComment;
            _sourceBackupFileXMLCollection = sourceBackupFileXMLCollection;
            _sourceDatabaseServer = sourceDatabaseServer;
            _sourceDatabaseName = sourceDatabaseName;
            _changeRecoveryToSimple = changeRecoveryToSimple;
            _positinInFileCollection = positinInFileCollection;
            _backupType = backupType;
            _restoreWithRecovery = restoreWithRecovery;

            Owner = databaseOwner;
            SupportsAbort = true;
            Name = string.Format("Restore {0} database on {1} server.", databaseName, databaseServer);
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
                                _commandRestore = conn.CreateCommand();
                                _commandRestore.CommandTimeout = 66000;
                                _commandRestore.CommandType = CommandType.StoredProcedure;
                                _commandRestore.CommandText = "dbo.RestoreDatabase";
                                _commandRestore.Parameters.Add(new SqlParameter("@DatabaseName", _databaseName));
                                _commandRestore.Parameters.Add(new SqlParameter("@FileCollection", _sourceBackupFileXMLCollection));
                                _commandRestore.Parameters.Add(new SqlParameter("@DatabaseOwner", _databaseOwner));
                                _commandRestore.Parameters.Add(new SqlParameter("@AdHocBackupRestore", false));
                                _commandRestore.Parameters.Add(new SqlParameter("@ChangeRecoveryToSimple", _changeRecoveryToSimple));
                                _commandRestore.Parameters.Add(new SqlParameter("@OriginatingServer", _sourceDatabaseServer));
                                _commandRestore.Parameters.Add(new SqlParameter("@OriginatingDatabaseName", _sourceDatabaseName));
                                _commandRestore.Parameters.Add(new SqlParameter("@PositinInFileCollection", _positinInFileCollection));
                                _commandRestore.Parameters.Add(new SqlParameter("@BackupTypeDescription", _backupType));
                                _commandRestore.Parameters.Add(new SqlParameter("@RestoreWithRecovery", _restoreWithRecovery));
                                _commandRestore.Parameters.Add(new SqlParameter("@DatabaseComment", String.IsNullOrWhiteSpace(_databaseComment) ? DBNull.Value : (object)_databaseComment));

                                conn.FireInfoMessageEventOnUserErrors = true;
                                conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                conn.Open();
                                _commandRestore.ExecuteNonQuery();
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
                if (_commandRestore != null)
                {
                    _commandRestore.Cancel();
                }
            }
            catch { };
        }
        

    }
}