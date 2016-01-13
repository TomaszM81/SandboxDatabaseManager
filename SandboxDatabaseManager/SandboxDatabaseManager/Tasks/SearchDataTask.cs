using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SandboxDatabaseManager.Tasks
{
    public class SearchDataTask : BaseBgTask
    {
        private string _databaseServerFilter;
        private string _databaseNameFilter;
        private string _sqlStatement;
        private DataSet _dsResult;
        private object _sync = new object();



        CancellationTokenSource _cancelToken = new CancellationTokenSource();

        public SearchDataTask(string databaseServerFilter, string databaseNameFilter, string sqlStatement, string owner)
        {
            _databaseServerFilter = databaseServerFilter;
            _databaseNameFilter = databaseNameFilter;
            _sqlStatement = sqlStatement;
            Description = sqlStatement;



            RedirectToAction = "ShowSearchResults";
            RedirectToController = "SearchData";
            Owner = owner;
            SupportsAbort = true;
            Name = string.Format("Search data on {0} and {1} databases.", databaseServerFilter, String.IsNullOrWhiteSpace(databaseNameFilter) ? "All" : databaseNameFilter);
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
                            _dsResult = new DataSet();
                            _dsResult.Tables.Add(new DataTable("Databases"));

                            AppendOutputText(String.Format("Starting data search...{0}", Environment.NewLine));

                            List<DatabaseServerInfo> listOfDatabaseServers;


                            if (_databaseServerFilter != "All Servers")
                            {
                                if (!UserPermissions.Instance.UserSpecificPermissions[Owner.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(_databaseServerFilter))
                                {
                                    listOfDatabaseServers = new List<DatabaseServerInfo>();
                                }
                                else
                                {
                                    listOfDatabaseServers = DatabaseServers.Instance.ItemsList.AsQueryable().Where(item => String.Compare(item.Name, _databaseServerFilter, true) == 0).ToList();
                                }
                            }
                            else
                            {
                                listOfDatabaseServers = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[Owner.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).ToList();
                            }

                            Parallel.ForEach(listOfDatabaseServers, (item, loopState) =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    loopState.Stop();
                                    return;
                                }


                                DataTable dtResultsDatabases = new DataTable("Databases");
                                dtResultsDatabases.Columns.Add(new DataColumn("Server Name", typeof(string)) { DefaultValue = item.Name });

                                try
                                {
                                    using (SqlConnection conn = new SqlConnection(item.ConnectionString + ";Connection Timeout=30"))
                                    {
                                        var command = conn.CreateCommand();
                                        command.CommandType = CommandType.StoredProcedure;
                                        command.CommandText = "dbo.ListDatabases";
                                        command.Parameters.AddWithValue("@DatabaseNameFilter", _databaseNameFilter);
                                        conn.Open();
                                        conn.FireInfoMessageEventOnUserErrors = true;
                                        conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);

                                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                                        adapter.Fill(dtResultsDatabases);
                                    }
                                }catch(Exception ex)
                                {
                                    this.AppendOutputText(ex.Message);
                                    Log.ErrorFormat("Exception while retrieving database list from server: {0}, exception:{1}", item.Name, ex.Message);
                                }

                                AppendOutputText(String.Format("Retrieved database list from server: {0}.{1}", item.Name, Environment.NewLine));
                                 


                                Parallel.ForEach(dtResultsDatabases.AsEnumerable(), (databaseRow, loopState2) =>
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        loopState2.Stop();
                                        return;
                                    }

                                    if (databaseRow["DatabaseState"].ToString().ToUpper() != "ONLINE")
                                        return;


                                    try
                                    {
                                        using (SqlConnection conn = new SqlConnection(item.ConnectionString + ";Connection Timeout=30"))
                                        {
                                            SqlCommand command = conn.CreateCommand();
                                            command.CommandTimeout = 120;
                                            command.CommandType = CommandType.StoredProcedure;
                                            command.CommandText = "dbo.ExecuteReadOnlyStatementAtDatabase";
                                            command.Parameters.Add(new SqlParameter("@SqlStatement", _sqlStatement));
                                            command.Parameters.Add(new SqlParameter("@DatabaseName", databaseRow["Database Name"]));

                                            conn.FireInfoMessageEventOnUserErrors = true;
                                            conn.InfoMessage += new SqlInfoMessageEventHandler(connInfoMessage);
                                            conn.Open();

                                            DataTable dtResults = new DataTable(String.Format("[{0}].[{1}]", item.Name, databaseRow["Database Name"]));

                                            SqlDataAdapter dataAdaper = new SqlDataAdapter(command);
                                            dataAdaper.Fill(dtResults);
                                            AppendOutputText(String.Format("Retrieved results from server: {0} database: {1}.{2}", item.Name, databaseRow["Database Name"], Environment.NewLine));

                                            if (dtResults.Rows.Count > 0)
                                            {
                                                var singleRowTable = dtResultsDatabases.Clone();
                                                singleRowTable.ImportRow(databaseRow);
                                                this.AddToResultsPool(singleRowTable, dtResults);
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        AppendOutputText(ex.Message);
                                        Log.ErrorFormat("Exception while executing statement on server: {0}, database:{1}, exception:{2}", item.Name, databaseRow["Database Name"], ex.Message);
                                    }

                                });


                            });


                            token.ThrowIfCancellationRequested();


                            if (_dsResult.Tables["Databases"].Rows.Count > 0)
                            {

                                _dsResult.Tables["Databases"].Columns.Remove("ID");
                                _dsResult.Tables["Databases"].Columns.Remove("Source Backup File Collection");
                                _dsResult.Tables["Databases"].Columns.Remove("Comment");
                                _dsResult.Tables["Databases"].Columns.Remove("Connections");

                                _dsResult.Tables["Databases"].DefaultView.Sort = "[Server Name] asc, [Database Name] asc";
                                DataTable temp = _dsResult.Tables["Databases"].DefaultView.ToTable("Databases");
                                _dsResult.Tables.Remove("Databases");
                                _dsResult.Tables.Add(temp);

                                this.Result = _dsResult;
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
                            Log.Error("Failed to search data on databases.", ex);
                        }
                    });
            }catch(Exception ex)
            {
                Log.Error("Unable to start task.", ex);
                throw;
            }
        }

        private void AddToResultsPool(DataTable databaseRow, DataTable dtResults)
        {
            lock(_sync)
            {
                _dsResult.Tables["Databases"].Merge(databaseRow);
                _dsResult.Tables.Add(dtResults);
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
            _cancelToken.Cancel();
        }


        public override void PopulateParametersForHistory(SqlCommand command)
        {
            string result = null;

            DataSet ds = this.Result as DataSet;
            if(ds != null)
            {
                StringWriter sw = new StringWriter();
                ds.WriteXml(sw);
                result = sw.ToString();
            }


            base.PopulateParametersForHistory(command);
            command.Parameters.AddWithValue("@Type", "S");
            command.Parameters.AddWithValue("@Result", result);
        }

    }

}