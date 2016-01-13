using SandboxDatabaseManager.Configuration;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using SandboxDatabaseManager.Worker;

namespace SandboxDatabaseManager.Tasks
{
    public class CompareDatabaseTask : BaseBgTask
    {
        private string _databaseServer;
        private string _databaseNameToCompare;
        private string _databaseNameToCompareAgaints;
        private string _listOfTablesToCompare;
        private int? _maxTableRowCount;
        private DataTable _dtResult;
        private object _sync = new object();
        private long _taskProgress = 0;



        CancellationTokenSource _cancelToken = new CancellationTokenSource();

        public CompareDatabaseTask(string databaseServer, string databaseNameToCompare, string databaseNameToCompareAgaints, string listOfTablesToCompare, int? maxTableRowCount, string owner)
        {
            _databaseServer = databaseServer;
            _databaseNameToCompare = databaseNameToCompare;
            _databaseNameToCompareAgaints = databaseNameToCompareAgaints;
            _listOfTablesToCompare = listOfTablesToCompare;
            _maxTableRowCount = maxTableRowCount;

            RedirectToAction = "ShowCompareResult";
            RedirectToController = "CompareDatabase";
            Owner = owner;
            SupportsAbort = true;
            Name = string.Format("Database Compare on {0} server between {1} and {2} databases.", databaseServer, databaseNameToCompare, databaseNameToCompareAgaints);
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
                            _dtResult = new DataTable();

                             var tableList = new List<Tuple<string, int, int, int>>().Select(item => new { FullTableName = item.Item1, HasSameColumns = item.Item2, HasPK = item.Item3, MaxRowCountFromBoth = item.Item4 }).ToList();

                            if (!UserPermissions.Instance.UserSpecificPermissions[Owner.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(_databaseServer))
                                throw new Exception("No permission to use this server.");


                            List<string> tableListRevided = new List<string>();
                            if (!String.IsNullOrWhiteSpace(_listOfTablesToCompare))
                                _listOfTablesToCompare.Replace("--", "").Split(',').ToList().ForEach(item => tableListRevided.Add(item.Trim()));


                            AppendOutputText(String.Format("Retrieving list of table to compare...{0}", Environment.NewLine));


                            var serverToUse = DatabaseServers.Instance.ItemsList.FirstOrDefault(item => item.Name == _databaseServer);


                            try
                            {
                                using (SqlConnection conn = new SqlConnection(serverToUse.ConnectionString + ";Connection Timeout=30"))
                                {
                                    SqlMetaData[] meta = new[]
                                    {
                                             new SqlMetaData("item", SqlDbType.NVarChar, 200),
                                    };


                                    var results = tableListRevided.Select(item =>
                                    {
                                        SqlDataRecord newRow = new SqlDataRecord(meta);
                                        newRow.SetValues(item);
                                        return newRow;
                                    });

                                    SqlCommand command = conn.CreateCommand();
                                    command.CommandTimeout = 5 * 60;
                                    command.CommandType = CommandType.StoredProcedure;
                                    command.CommandText = "dbo.GetTablesForDataCompare";
                                    command.Parameters.Add(new SqlParameter("@FirstDatabaseName", _databaseNameToCompare));
                                    command.Parameters.Add(new SqlParameter("@SecondDatabaseName", _databaseNameToCompareAgaints));
                                    SqlParameter param1 = new SqlParameter("@TablesToCompare", SqlDbType.Structured);
                                    param1.TypeName = "dbo.StringList";
                                    param1.Value = tableListRevided.Count > 0 ? results : null;
                                    command.Parameters.Add(param1);

                                    conn.Open();
                                    SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);


                                    while (myReader.Read())
                                    {
                                        tableList.Add( new { FullTableName =  myReader["FullTableName"].ToString(), HasSameColumns = Int32.Parse(myReader["HasSameColumns"].ToString()), HasPK = Int32.Parse(myReader["HasPK"].ToString()), MaxRowCountFromBoth = Int32.Parse(myReader["MaxRowCountFromBoth"].ToString()) });
                                    }
                                    conn.Close();
                                    AppendOutputText(String.Format("Finished retrieving list of table.{0}", Environment.NewLine));
                                }
                            }
                            catch (Exception ex)
                            {
                                Status = TaskStatus.Failed;
                                this.AppendOutputText(ex.Message);
                                Log.ErrorFormat("Exception while retrieving list of table to compare from server: {0}, exception:{1}", serverToUse.Name, ex.Message);
                                return;
                            }


                            token.ThrowIfCancellationRequested();

                            int totalCount = tableList.Count;

                            if (totalCount == 0)
                                AppendOutputText(String.Format("There are no tables to compare.{0}", Environment.NewLine));


                            Parallel.ForEach(tableList, (item, loopState) =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    loopState.Stop();
                                    return;
                                }

                                if (item.HasSameColumns == 0)
                                {
                                    AppendOutputText(String.Format("Finished {1,4} of {2}. Table {0} will not be compared, column schema difference.{3}", item.FullTableName, Interlocked.Increment(ref _taskProgress), totalCount, Environment.NewLine));

                                }
                                else if (item.HasPK == 0)
                                {
                                    AppendOutputText(String.Format("Finished {1,4} of {2}. Table {0} will not be compared, missing primary key.{3}", item.FullTableName, Interlocked.Increment(ref _taskProgress), totalCount, Environment.NewLine));
                                }
                                else if (item.MaxRowCountFromBoth == 0)
                                {
                                    AppendOutputText(String.Format("Finished {1,4} of {2}. Table {0} will not be compared, empty on both databases.{3}", item.FullTableName, Interlocked.Increment(ref _taskProgress), totalCount, Environment.NewLine));
                                }
                                else if (_maxTableRowCount.HasValue && item.MaxRowCountFromBoth > _maxTableRowCount.Value)
                                {
                                    AppendOutputText(String.Format("Finished {1,4} of {2}. Table {0} will not be compared, rowcount exceding specified limit of {4}.{3}", item.FullTableName, Interlocked.Increment(ref _taskProgress), totalCount, Environment.NewLine, _maxTableRowCount.Value));
                                }
                                else
                                {
                                    try
                                    {
                                        DataTable dtResults = new DataTable(item.FullTableName);
                                        using (SqlConnection conn = new SqlConnection(serverToUse.ConnectionString + ";Connection Timeout=30"))
                                        {

                                            SqlCommand command = conn.CreateCommand();
                                            command.CommandTimeout = 30 * 60;
                                            command.CommandType = CommandType.StoredProcedure;
                                            command.CommandText = "dbo.CompareDataInTable";
                                            command.Parameters.Add(new SqlParameter("@TableName", item.FullTableName));
                                            command.Parameters.Add(new SqlParameter("@FirstDatabaseName", _databaseNameToCompare));
                                            command.Parameters.Add(new SqlParameter("@SecondDatabaseName", _databaseNameToCompareAgaints));
                                            SqlDataAdapter dataAdaper = new SqlDataAdapter(command);
                                            dataAdaper.Fill(dtResults);
                                            AppendOutputText(String.Format("Finished {1,4} of {2}. Finished comparing data for table {0}.{3}", item.FullTableName, Interlocked.Increment(ref _taskProgress), totalCount, Environment.NewLine));
                                        }

                                        if (dtResults.Rows.Count > 0)
                                        {
                                            lock (_sync)
                                            {
                                                _dtResult.Merge(dtResults);
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        AppendOutputText(String.Format("Exception during CompareDataInTable for table {0}. Exception:{1}{2}", item.FullTableName, ex.Message, Environment.NewLine));
                                        Log.ErrorFormat("Exception during CompareDataInTable for table {0}. Exception:{1}", item.FullTableName, ex.Message);
                                    }

                                }


                            });

                            token.ThrowIfCancellationRequested();


                            if (_dtResult.Rows.Count > 0)
                            {
                                _dtResult.DefaultView.Sort = "[TableName] asc, [TableKeyValues] asc";
                                this.Result = _dtResult.DefaultView.ToTable("Result");
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
                            Log.Error("Failed to compare databases.", ex);
                        }
                    });
            }catch(Exception ex)
            {
                Log.Error("Unable to start task.", ex);
                throw;
            }
        }

        public override void RequestAbort()
        {
            _cancelToken.Cancel();
        }

        public override void PopulateParametersForHistory(SqlCommand command)
        {
            string result = null;

            DataTable dt = this.Result as DataTable;
            if (dt != null)
            {
                StringWriter sw = new StringWriter();
                dt.WriteXml(sw, XmlWriteMode.WriteSchema);
                result = sw.ToString();
            }


            base.PopulateParametersForHistory(command);
            command.Parameters.AddWithValue("@Type", "T");
            command.Parameters.AddWithValue("@Result", result);
        }


    }
}