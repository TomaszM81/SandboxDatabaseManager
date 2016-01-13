using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SandboxDatabaseManager.Worker
{
	public class DatabaseServerSettings
	{
        public enum Setting
        {
            SetRecoveryModelToSimpleDefault
        }

        protected static Dictionary<DatabaseServerSettings.Setting, object> _settingDefaulter = new Dictionary<Setting, object> () { { Setting.SetRecoveryModelToSimpleDefault, true } };

        public class DatabaseServerSettingsCollection
        {
            private Dictionary<DatabaseServerSettings.Setting, object> _setting = new Dictionary<Setting, object>();
            public object GetSetting(DatabaseServerSettings.Setting setting)
            {
                if(_setting.ContainsKey(setting))
                {
                    return _setting[setting];
                }
                else
                {
                    return DatabaseServerSettings._settingDefaulter[setting];
                }
            }
            public void Add(DatabaseServerSettings.Setting setting, object value)
            {
                _setting[setting] = value;
            }
		}

		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Lazy<DatabaseServerSettings> _instance = new Lazy<DatabaseServerSettings>(() => new DatabaseServerSettings());
		private Task backgroundTask;
		private static object lockObj = new object();
		private Dictionary<string, DatabaseServerSettingsCollection> _databaseServerSettingsMap = new Dictionary<string, DatabaseServerSettingsCollection>();
		public Dictionary<string, DatabaseServerSettingsCollection> DatabaseServerSettingsMap
        {
			get
			{
				return _databaseServerSettingsMap;
			}
		}



		public static void Initialize()
		{
			var dummmy = Instance;
		}

		public static DatabaseServerSettings Instance
		{
			get
			{
				lock (lockObj)
				{
					return _instance.Value;
				}
			}
		}

		private DatabaseServerSettings()
		{
			backgroundTask = new Task(Worker, TaskCreationOptions.LongRunning);
			backgroundTask.Start();
		}

		async void Worker()
		{
			while (true)
			{
				try
				{

					if (!Reinitialize())
					{
						Log.Error("Error while refreshing database server settings.");
						break;
					}

					await Task.Delay(TimeSpan.FromMinutes(60));

				}
				catch (Exception ex)
				{
					Log.Error("Exception in DatabaseServerSettings.", ex);
				}
			}
		}

		private bool Reinitialize()
		{



			try
			{


				Dictionary<string, DatabaseServerSettingsCollection> databaseServerSettingsMapCandidate = new Dictionary<string, DatabaseServerSettingsCollection>();




				object sync = new object();

				Parallel.ForEach(DatabaseServers.Instance.ItemsList, otherServer =>
                {
                    lock (sync)
                    {
                        databaseServerSettingsMapCandidate.Add(otherServer.Name, new DatabaseServerSettingsCollection());
                    }

                    try
					{
						using (SqlConnection conn = new SqlConnection(otherServer.ConnectionString + ";Connection Timeout=30"))
						{
							SqlCommand command = conn.CreateCommand();
							command.CommandType = CommandType.Text;
							command.CommandText = @"SELECT " +
                                                        "SetRecoveryModelToSimpleDefault " +
                                                    "FROM dbo.Configuration WHERE SetRecoveryModelToSimpleDefault IS NOT NULL";

							conn.Open();
							SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);
							while (myReader.Read())
							{

                              lock (sync)
                              {
                                  databaseServerSettingsMapCandidate[otherServer.Name].Add(Setting.SetRecoveryModelToSimpleDefault, bool.Parse(myReader["SetRecoveryModelToSimpleDefault"].ToString()));

                              }
                            }
						}

					}
					catch (Exception ex)
					{
						Log.ErrorFormat("Exception while retrieving settings from: {0}, {1}.", otherServer.Name, ex.Message);
					}
				});



				lock (lockObj)
				{
					_databaseServerSettingsMap = databaseServerSettingsMapCandidate;
				}

                return true;

			}
			catch (Exception ex)
			{
				Log.Error("Failed to retrieve setting list from database.", ex);
				return false;
			}


		}
	}
}