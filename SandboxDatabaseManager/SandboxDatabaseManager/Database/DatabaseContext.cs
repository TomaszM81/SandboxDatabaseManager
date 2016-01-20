using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Models;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using General.Security;
using System.Security.Cryptography;
using System.Security;
using SandboxDatabaseManager.Tasks;
using SandboxDatabaseManager.Worker;

namespace SandboxDatabaseManager.Database
{
    internal class DatabaseContext
    {

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static DataSet ListDatabases(string databaseServerFilter = null, string databaseNameFilter = null, string databaseOwner = null, string indexDatabasesInContextOfUser = null)
        {
            try
            {
                if(String.IsNullOrWhiteSpace(databaseServerFilter))
                {
                    var data = new DataSet();
                    return data;
                }

                List<DatabaseServerInfo> list = null;

                

                if(indexDatabasesInContextOfUser != null)
                {
                    if(databaseServerFilter != "All Servers")
                    {
                        if(!UserPermissions.Instance.UserSpecificPermissions[indexDatabasesInContextOfUser].CopyAndSearchFromDatabaseSeverList.Contains(databaseServerFilter))
                        {
                            var data = new DataSet();
                            return data;
                        }

                        list = DatabaseServers.Instance.ItemsList.AsQueryable().Where(item => String.Compare(item.Name, databaseServerFilter, true) == 0).ToList();
                    }
                    else
                    {
                        list = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[indexDatabasesInContextOfUser].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).ToList();
                    }

                }else if(databaseOwner != null)
                {
                    databaseOwner = databaseOwner.ToUpper();
                    if (databaseServerFilter != "All Servers")
                    {
                        if (!UserPermissions.Instance.UserSpecificPermissions[databaseOwner].RestoreToServerList.Contains(databaseServerFilter))
                        {
                            var data = new DataSet();
                            return data;
                        }

                        list = DatabaseServers.Instance.ItemsList.AsQueryable().Where(item => String.Compare(item.Name, databaseServerFilter, true) == 0).ToList();
                    }
                    else
                    {
                        list = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[databaseOwner].RestoreToServerList.Contains(server.Name)).ToList();
                    }
                }

                object mylock = new object();
                List<DataTable> serverResultsDatabases = new List<DataTable>();
                List<DataTable> serverResultsDatabaseSnapshots = new List<DataTable>();


                Parallel.ForEach(list, server =>
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                        {
                            var command = conn.CreateCommand();
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "dbo.ListDatabases";
                            command.Parameters.AddWithValue("@DatabaseNameFilter", databaseNameFilter);
                            command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                            conn.Open();

                            DataTable dtResultsDatabases = new DataTable("Databases");
                            DataTable dtResultsDatabaseSnapshots = new DataTable("DatabaseSnapshots");

                            dtResultsDatabases.Columns.Add(new DataColumn("Server Name", typeof(string)) { DefaultValue = server.Name });
                            dtResultsDatabaseSnapshots.Columns.Add(new DataColumn("Server Name", typeof(string)) { DefaultValue = server.Name });
                            
                            DataSet dsResult = new DataSet();
                            dsResult.Tables.Add(dtResultsDatabases);
                            dsResult.Tables.Add(dtResultsDatabaseSnapshots);


                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            adapter.TableMappings.Add("Table","Databases");
                            adapter.TableMappings.Add("Table1","DatabaseSnapshots");
                            adapter.Fill(dsResult);
                            

                            lock (mylock)
                            {
                                serverResultsDatabases.Add(dsResult.Tables["Databases"]);
                                serverResultsDatabaseSnapshots.Add(dsResult.Tables["DatabaseSnapshots"]);
                            }
                        }
                    }catch(Exception ex)
                    {
                        Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    }

                });

                serverResultsDatabases.Sort(delegate(DataTable t1, DataTable t2) { return (t1.TableName.CompareTo(t2.TableName)); });

                DataTable finalResultDatabases = new DataTable("Databases");
                foreach (var item in serverResultsDatabases)
                {
                    finalResultDatabases.Merge(item, false, MissingSchemaAction.Add);
                }

                finalResultDatabases.Columns["Created On"].SetOrdinal(finalResultDatabases.Columns.Count - 1);


                DataTable finalResultDatabaseSnapshots = new DataTable("DatabaseSnapshots");
                foreach (var item in serverResultsDatabaseSnapshots)
                {
                    finalResultDatabaseSnapshots.Merge(item, false, MissingSchemaAction.Add);
                }

                DataSet dsFinal = new DataSet();
                dsFinal.Tables.Add(finalResultDatabases);
                dsFinal.Tables.Add(finalResultDatabaseSnapshots);


                return dsFinal;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return null;

        }

        public static BackupFilesResult ListBackupFiles(string listInContextOfUser, string locationFilter = null, string fileNameFiler = null, bool includeFull = true, bool includeDiff = false, bool includeLog = false)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(locationFilter))
                    return new BackupFilesResult() { BackupFiles = new DataTable(), InfoMessage = String.Empty };


                List<DatabaseServerInfo> listOfServersToUse = DatabaseServers.Instance.ItemsList.Where(server => server.UseForBackupFileScan).ToList();
                if (listOfServersToUse.Count == 0)
                    throw new Exception("There are no servers configured that can be used for backup file scanning, please configure at least one");


                var investigationList = new List<Tuple<DatabaseServerInfo, string, string, string, bool, bool, bool>>().Select(t => new { ServerToUse = t.Item1, LocationName = t.Item2 ,PathToInvestigate = t.Item3, FileFilter = t.Item4, includeFull = t.Item5, includeDiff = t.Item6, includeLog = t.Item7 }).ToList();
       
                if(locationFilter != "All locations")
                {
                    // just one location then

                    var result = listOfServersToUse.FirstOrDefault(server => server.Name == locationFilter);
                    if (result != null)
                        investigationList.Add(new { ServerToUse = result, LocationName = result.Name, PathToInvestigate = result.BackupDatabaseNetworkSharePath, FileFilter = fileNameFiler, includeFull = includeFull, includeDiff = includeDiff, includeLog = includeLog });
                    else
                    {
                        result = DatabaseServers.Instance.ItemsList.FirstOrDefault(server => server.Name == locationFilter);
                        if(result != null)
                        {
                            investigationList.Add(new { ServerToUse = listOfServersToUse[0], LocationName = result.Name, PathToInvestigate = result.BackupDatabaseNetworkSharePath, FileFilter = fileNameFiler, includeFull = includeFull, includeDiff = includeDiff, includeLog = includeLog });
                        }
                        else
                        {
                            var locationResult = DatabaseBackupFileLocations.Instance.ItemsList.FirstOrDefault(server => server.Name == locationFilter);
                            if(locationResult != null)
                            {
                                investigationList.Add(new { ServerToUse = listOfServersToUse[0], LocationName = locationResult.Name, PathToInvestigate = locationResult.Path, FileFilter = fileNameFiler, includeFull = includeFull, includeDiff = includeDiff, includeLog = includeLog });
                            }

                        }

                    }


                }
                else
                {



                    listOfServersToUse.Where(server => !String.IsNullOrWhiteSpace(server.BackupDatabaseNetworkSharePath) && UserPermissions.Instance.UserSpecificPermissions[listInContextOfUser].RestoreFromDatabaseServerList.Contains(server.Name)).ToList().ForEach(server =>
                    {
                        investigationList.Add(new { ServerToUse = server, LocationName = server.Name,  PathToInvestigate = server.BackupDatabaseNetworkSharePath, FileFilter = fileNameFiler, includeFull = includeFull, includeDiff = includeDiff, includeLog = includeLog });
                    });

                    int i = 0;
                    foreach(var server in DatabaseServers.Instance.ItemsList.Where(server => !server.UseForBackupFileScan && !String.IsNullOrWhiteSpace(server.BackupDatabaseNetworkSharePath) && UserPermissions.Instance.UserSpecificPermissions[listInContextOfUser].RestoreFromDatabaseServerList.Contains(server.Name)))
                    {
                        // distribute
                        investigationList.Add(new
                        { ServerToUse = listOfServersToUse[i++ % listOfServersToUse.Count],
                            LocationName = server.Name,
                            PathToInvestigate = server.BackupDatabaseNetworkSharePath,
                            FileFilter = fileNameFiler,
                            includeFull = includeFull,
                            includeDiff = includeDiff,
                            includeLog = includeLog
                        });
                    }

                    foreach (var location in DatabaseBackupFileLocations.Instance.ItemsList)
                    {
                        // distribute
                        investigationList.Add(new
                        {
                            ServerToUse = listOfServersToUse[i++ % listOfServersToUse.Count],
                            LocationName = location.Name,
                            PathToInvestigate = location.Path,
                            FileFilter = fileNameFiler,
                            includeFull = includeFull,
                            includeDiff = includeDiff,
                            includeLog = includeLog
                        });
                    }


                }



                object mylock = new object();
                List<DataTable> backupFilesResult = new List<DataTable>();
                string finalInfoMessage = "";


                Parallel.ForEach(investigationList, item =>
                {
                    try
                    {
                        DataTable dtResultsFiles = new DataTable("BackupFiles");
                        dtResultsFiles.Columns.Add(new DataColumn("Location Name", typeof(string)) { DefaultValue = item.LocationName });
                        SqlParameter sqlOutputFileName = new SqlParameter("@LogMessage", SqlDbType.VarChar, -1);
                        sqlOutputFileName.Direction = ParameterDirection.Output;

                        using (SqlConnection conn = new SqlConnection(item.ServerToUse.ConnectionString))
                        {
                            var command = conn.CreateCommand();
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandTimeout = 900;
                            command.CommandText = "dbo.GetBackupsInLocation";
                            command.Parameters.AddWithValue("@LocationToInvestigate", item.PathToInvestigate);
                            command.Parameters.AddWithValue("@FileNameFilter", item.FileFilter);
                            command.Parameters.AddWithValue("@IncludeFull", item.includeFull);
                            command.Parameters.AddWithValue("@IncludeDiff", item.includeDiff);
                            command.Parameters.AddWithValue("@IncludeLog", item.includeLog);
                            command.Parameters.Add(sqlOutputFileName);

                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            adapter.Fill(dtResultsFiles);
                        }

                        lock (mylock)
                        {
                            backupFilesResult.Add(dtResultsFiles);
                            finalInfoMessage += sqlOutputFileName.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(String.Format("Error from server {0}, during backup file scan operation.", item.ServerToUse.Name), ex);
                    }

                });

              

                DataTable finalResultFiles = new DataTable("BackupFiles");
                foreach (var item in backupFilesResult)
                {
                    finalResultFiles.Merge(item, false, MissingSchemaAction.Add);
                }

                finalResultFiles.DefaultView.Sort = "[Location Name] asc, [File Name] asc";
                DataTable temp = finalResultFiles.DefaultView.ToTable("BackupFiles");
                finalResultFiles = temp;
                


                return new BackupFilesResult() { BackupFiles = finalResultFiles, InfoMessage = finalInfoMessage };

            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return null;

        }

        public static ValidateRestoreOperationResult ValidateRestoreOperation(string databaseServer, string databaseName, string databaseOwner, string backupTypeDescription = "Database", decimal? firstLSN = null, decimal? lastLSN = null, decimal? databaseBackupLSN = null)
        {
            ValidateRestoreOperationResult result = new ValidateRestoreOperationResult();
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.ValidateRestoreOperation";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);


                        command.Parameters.AddWithValue("@BackupTypeDescription", backupTypeDescription);
                        command.Parameters.AddWithValue("@FirstLSN", firstLSN.HasValue ? firstLSN.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LastLSN", lastLSN.HasValue ? lastLSN.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DatabaseBackupLSN", databaseBackupLSN.HasValue ? databaseBackupLSN.Value : (object)DBNull.Value);
                        

                        var param = new SqlParameter("@CanOverwrite", SqlDbType.Bit);
                        param.Direction = ParameterDirection.Output;
                        command.Parameters.Add(param);

                        var paramCustomErrorMessage = new SqlParameter("@CustomErrorMessage", SqlDbType.VarChar, 1024);
                        paramCustomErrorMessage.Direction = ParameterDirection.Output;
                        command.Parameters.Add(paramCustomErrorMessage);


                        conn.Open();
                        command.ExecuteNonQuery();


                        result.CanOverwrite = param.Value == DBNull.Value ? null : (bool?)param.Value;
                        result.CustomErrorMessage = paramCustomErrorMessage.Value == DBNull.Value ? null : (string)paramCustomErrorMessage.Value;


                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }

               
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }


        }

        public static Tuple<bool, bool> ValidateTargetDatabaseSnaphotName(string databaseServer, string databaseSnapshotName, string databaseOwner)
        {
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.ValidateDatabaseSnaphotName";
                        command.Parameters.AddWithValue("@DatabaseSnapshotName", databaseSnapshotName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        var param = new SqlParameter("@CanOverwrite", SqlDbType.Bit);
                        param.Direction = ParameterDirection.Output;
                        command.Parameters.Add(param);
                        conn.Open();
                        command.ExecuteNonQuery();


                        if (param.Value != DBNull.Value)
                        {
                            return Tuple.Create<bool, bool>(false, (bool)param.Value);
                        }

                        return Tuple.Create<bool, bool>(true, false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }


        }
        public static void ChangeDatabaseComment(string databaseServer, string databaseName, string databaseOwner, string databaseComment)
        {
            

            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.ChangeDatabaseComment";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        command.Parameters.AddWithValue("@DatabaseComment", databaseComment);
                        conn.Open();
                        command.ExecuteNonQuery();

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }

               
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static void TransferDatabaseToUser(string databaseServer, string databaseName, string databaseOwner, string newDatabaseOwner)
        {


            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.TransferDatabaseToUser";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        command.Parameters.AddWithValue("@NewDatabaseOwner", newDatabaseOwner);
                        conn.Open();
                        command.ExecuteNonQuery();

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static void KillConnectionsToPersonalDatabase(string databaseServer, string databaseName, string databaseOwner)
        {


            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.KillConnectionsToPersonalDatabase";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        conn.Open();
                        command.ExecuteNonQuery();

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static long GetLatestRevisionChangeTracking(string databaseServer, string databaseName)
        {
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GetLatestRevisionChangeTracking";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        conn.Open();
                        
                        return (long)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static int? CheckIfTrackingEnabled(string databaseServer, string databaseName, string databaseOwner)
        {
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.CheckIfTrackingEnabled";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        conn.Open();

                        return (int?)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static List<string> GetTablesForDataTrack(string databaseServer, string databaseName, string tablesToTrackCommaSeparated, string databaseOwner)
        {
            try
            {
                List<string> sqlStatementList = new List<string>();
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                List<string> tablesToTrackList = new List<string>();

                if (!String.IsNullOrWhiteSpace(tablesToTrackCommaSeparated))
                    tablesToTrackCommaSeparated.Replace("--", "").Split(',').ToList().ForEach(item => tablesToTrackList.Add(item.Trim()));


                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        SqlMetaData[] meta = new [] { new SqlMetaData("item", SqlDbType.NVarChar, 200)};


                        var results = tablesToTrackList.Select(item =>
                        {
                            SqlDataRecord newRow = new SqlDataRecord(meta);
                            newRow.SetValues(item);
                            return newRow;
                        });

                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GetTablesForDataTrack";
                        command.Parameters.AddWithValue("@DatabaseName", databaseName);
                        command.Parameters.AddWithValue("@DatabaseOwner", databaseOwner);
                        SqlParameter param1 = new SqlParameter("@TableNames", System.Data.SqlDbType.Structured);
                        param1.TypeName = "dbo.StringList";
                        param1.Value = tablesToTrackList.Count > 0 ? results : null;
                        command.Parameters.Add(param1);

                        conn.Open();
                        SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);
                        while (myReader.Read())
                        {
                            sqlStatementList.Add(myReader.GetString(0));
                        }
                    }

                    return sqlStatementList;
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                    throw;
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static DataTable TrackDataChanges(string databaseServer, string databaseName, string databaseOwner, List<string> sqlTrackingStatements, int? revision, out string logMessage)
        {
            try
            {

                DataTable dtResultsFinal = new DataTable();
                object sync = new object();
                object sync2 = new object();

                StringBuilder sbLog = new StringBuilder();


                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => String.Compare(item.Name, databaseServer, true) == 0);

                Parallel.ForEach(sqlTrackingStatements, statement =>
                {
                    try
                    {

                        DataTable dtResultsLocal = new DataTable("Resuls");
                        using (SqlConnection conn = new SqlConnection(server.ConnectionString + ";Connection Timeout=30"))
                        {

                            SqlCommand command = conn.CreateCommand();
                            command.CommandTimeout = 120;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "dbo.GetTableDataChanges";
                            command.Parameters.Add(new SqlParameter("@DatabaseName", databaseName));
                            command.Parameters.Add(new SqlParameter("@SlqStatement", statement));
                            command.Parameters.Add(new SqlParameter("@Revision", revision.HasValue ? revision.Value : 0));
                            SqlDataAdapter dataAdaper = new SqlDataAdapter(command);
                            dataAdaper.Fill(dtResultsLocal);
                            
                        }

                        if (dtResultsLocal.Rows.Count > 0)
                        {
                            lock (sync)
                            {
                                dtResultsFinal.Merge(dtResultsLocal);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock(sync2)
                        {
                            sbLog.Append(String.Format("{2}Exception while executing sql statement:{2}{0}.{2}Exception:{1}", statement, ex.Message, Environment.NewLine));
                        }
                        Log.ErrorFormat("Exception while executing sql statement: {0}. Exception:{1}", statement, ex.Message);
                    }

                });

                if (dtResultsFinal.Rows.Count > 0)
                {
                    dtResultsFinal.DefaultView.Sort = "TableName asc";
                    dtResultsFinal = dtResultsFinal.DefaultView.ToTable();
                }

                logMessage = sbLog.ToString();
                return dtResultsFinal;

            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }
        
        public static void InsertTaskHistory(IBgTask task)
        {
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {
                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.InsertTaskHistory";
                        task.PopulateParametersForHistory(command);
                        conn.Open();
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }





        }

        public static void RemoveTaskHistory(IBgTask task)
        {
            try
            {
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {

                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.DeleteFromTaskHistory";
                        command.Parameters.Add(new SqlParameter("ID", task.ID));
                        conn.Open();
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }





        }

        public static List<IBgTask> GetTaskHistoryFromDB()
        {
            List<IBgTask> list = new List<IBgTask>();

            try
            {
               
                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                using (SqlConnection conn = new SqlConnection(server.ConnectionString + ";Connection Timeout=30"))
                {
                    SqlCommand command = conn.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = "SELECT th.ID, th.Name, th.Type, th.OutputText, th.Owner, th.Result, th.Status, th.RedirectToController, th.RedirectToAction, th.Description, th.StartDate, th.EndDate FROM dbo.TaskHistory AS th";

                    conn.Open();
                    SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);
                    while (myReader.Read())
                    {

                        var histTask = new TaskHist()
                        {
                            Description = myReader["Description"] == DBNull.Value ? null : (string)myReader["Description"],
                            EndDate = (DateTime)myReader["EndDate"],
                            DiscardedFromStatsReporting = true,
                            ID = (string)myReader["ID"],
                            Name = (string)myReader["Name"],
                            OutputText = myReader["OutputText"] == DBNull.Value ? null : (string)myReader["OutputText"],
                            Owner = (string)myReader["Owner"],
                            RedirectOnlyResult = null,
                            RedirectToAction = myReader["RedirectToAction"] == DBNull.Value ? null : (string)myReader["RedirectToAction"],
                            RedirectToController = myReader["RedirectToController"] == DBNull.Value ? null : (string)myReader["RedirectToController"],
                            StartDate = (DateTime)myReader["EndDate"],
                            Status = (Tasks.TaskStatus)myReader["Status"]
                        };

                        string result = myReader["Result"] == DBNull.Value ? String.Empty : (string)myReader["Result"];
                        if (result.Length > 0)
                        {
                            StringReader sr = new StringReader(result);
                            string type = myReader["Type"] == DBNull.Value ? String.Empty : (string)myReader["Type"];

                            if (type == "T")
                            {
                                DataTable table = new DataTable();
                                table.ReadXml(sr);
                                histTask.Result = table;
                            }else if (type == "S")
                            {
                                DataSet ds = new DataSet();
                                ds.ReadXml(sr);
                                histTask.Result = ds;
                            }
                        }

                        list.Add(histTask);


                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }


            return list;

        }

        public static void StoreCountersData(DataTable statsToStore)
        {
            try
            {




                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {

                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.StoreCountersData";
                        SqlParameter parameter = new SqlParameter("CountersToStore", statsToStore);
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "dbo.CountersData";
                        command.Parameters.Add(parameter);

                        conn.Open();
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

        }

        public static DataTable GetCountersDataMinDates()
        {
            DataTable result = new DataTable();
            try
            {

                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {

                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GetCountersDataMinDates";
                        SqlDataAdapter da = new SqlDataAdapter(command);
                        da.Fill(result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return result;

        }


        public static DataTable GetCountersData(string serverFriendlyName, string counterFriendlyName, DateTime? startDate = null, DateTime? endDate = null)
        {
            DataTable result = new DataTable();
            try
            {

                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {

                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GetCountersData";
                        SqlDataAdapter da = new SqlDataAdapter(command);

                        command.Parameters.Add(new SqlParameter("ServerFriendlyName", serverFriendlyName));
                        command.Parameters.Add(new SqlParameter("CounterFriendlyName", counterFriendlyName));
                        command.Parameters.Add(new SqlParameter("DateStart", startDate.HasValue ? startDate.Value : (object)DBNull.Value));
                        command.Parameters.Add(new SqlParameter("DateEnd", endDate.HasValue ? endDate.Value : (object)DBNull.Value));
                        da.Fill(result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return result;

        }

        public static DataTable GetDateRangeForCounter(string serverFriendlyName, string counterFriendlyName)
        {
            DataTable result = new DataTable();
            try
            {

                DatabaseServerInfo server = DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary == true);

                try
                {

                    using (SqlConnection conn = new SqlConnection(server.ConnectionString))
                    {
                        var command = conn.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "dbo.GetDateRangeForCounter";
                        SqlDataAdapter da = new SqlDataAdapter(command);

                        command.Parameters.Add(new SqlParameter("ServerFriendlyName", serverFriendlyName));
                        command.Parameters.Add(new SqlParameter("CounterFriendlyName", counterFriendlyName));
                        da.Fill(result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(String.Format("Problem accessing server: {0}.", server.Name), ex);
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return result;

        }


    }
}
