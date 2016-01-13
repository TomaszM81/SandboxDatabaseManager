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
	internal class UserPermissions
	{
        static object _sync = new object();
        static private DateTime _permsTimeStamp = DateTime.MaxValue;
        internal static DateTime PermsTimeStamp
        {
            get
            {
                lock(_sync)
                {
                    return _permsTimeStamp;
                }
            }
            set
            {
                lock (_sync)
                {
                    _permsTimeStamp = value;
                }
            }
        }




        internal class PermissionsForUser
		{
            public List<string> BackupToDatabaseServerList { get; set; }
            public List<string> RestoreToServerList {get; set;}
			public List<string> BackupFromDatabaseServerList { get; set; }
            public List<string> RestoreFromDatabaseServerList { get; set; }
            public List<string> CopyAndSearchFromDatabaseSeverList { get; set; }
            public List<string> RestoreFromServerList { get; set; }
            public bool HasOverwriteBackupDestinationPermission { get; set; }
		}

		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Lazy<UserPermissions> _instance = new Lazy<UserPermissions>(() => new UserPermissions());
		private Task backgroundTask;
		private static object lockObj = new object();
		private Dictionary<string, PermissionsForUser> _userSpecificPermissions = new Dictionary<string,PermissionsForUser>();
        internal Dictionary<string, PermissionsForUser> UserSpecificPermissions
		{
			get
			{
				return _userSpecificPermissions;
			}
		}

        internal bool HasSandboxDatabaseManagerAccess(string domainUserName)
		{
			return UserSpecificPermissions.ContainsKey(domainUserName.ToUpper());
		}

        internal static void Initialize()
		{
			var dummmy = Instance;
		}

        internal static UserPermissions Instance
		{
			get
			{
				lock (lockObj)
				{
					return _instance.Value;
				}
			}
		}

		private UserPermissions()
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
						Log.Error("Error while refreshing user permissions.");
						break;
					}

					await Task.Delay(TimeSpan.FromMinutes(60));

				}
				catch (Exception ex)
				{
					Log.Error("Exception in UserList.", ex);
				}
			}
		}

		private bool Reinitialize()
		{



			try
			{


				Dictionary<string, PermissionsForUser> userSpecificPermissionsCandidate = new Dictionary<string,PermissionsForUser>();


				var mainFrontEndSrver =  DatabaseServers.Instance.ItemsList.FirstOrDefault(item => item.IsPrimary == true);

				if (mainFrontEndSrver == null)
					throw new Exception("No database server has been set as primary in the configuration file.");

                if (DatabaseServers.Instance.ItemsList.Count(item => item.IsPrimary == true) > 1)
                    throw new Exception("Not more than one server can be marked with IsPrimary = true in DatabaseServersList section of configuration file.");


                using (SqlConnection conn = new SqlConnection(mainFrontEndSrver.ConnectionString + ";Connection Timeout=30"))
				{
					SqlCommand command = conn.CreateCommand();
					command.CommandType = CommandType.Text;
					command.CommandText = @"SELECT " +
											"DomainUserName, " +
											"PrimaryOnlyHasOverwriteBackupDestinationPermission, " +
                                            "HasCopyAndSearchFromPermission, " +
                                            "HasBackupFromPermission, " +
                                            "HasRestoreToPermission, " +
                                            "HasBackupToPermission, " +
                                            "HasRestoreFromPermission, " +
                                            "GETUTCDATE() TimeStampDate " +
                                            "FROM dbo.Users WHERE PrimaryOnlyHasSandboxDatabaseManagerAccessPermission = 1"; 

					conn.Open();
					SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);
					while (myReader.Read())
					{
                        if (PermsTimeStamp > (DateTime)myReader["TimeStampDate"])
                            PermsTimeStamp = (DateTime)myReader["TimeStampDate"];


                        userSpecificPermissionsCandidate.Add(myReader["DomainUserName"].ToString().ToUpper(), new PermissionsForUser()
                        {
                            CopyAndSearchFromDatabaseSeverList = new List<string>(),
                            BackupFromDatabaseServerList = new List<string>(),
                            RestoreToServerList = new List<string>(),
                            BackupToDatabaseServerList = new List<string>(),
                            RestoreFromDatabaseServerList = new List<string>(),
                        });

                        if (bool.Parse(myReader["PrimaryOnlyHasOverwriteBackupDestinationPermission"].ToString()))
                            userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].HasOverwriteBackupDestinationPermission = true;

                        if (bool.Parse(myReader["HasCopyAndSearchFromPermission"].ToString()))
                            userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].CopyAndSearchFromDatabaseSeverList.Add(mainFrontEndSrver.Name);

                        if (bool.Parse(myReader["HasBackupFromPermission"].ToString()))
                            userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].BackupFromDatabaseServerList.Add(mainFrontEndSrver.Name);

                        if (bool.Parse(myReader["HasRestoreToPermission"].ToString()))
							userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].RestoreToServerList.Add(mainFrontEndSrver.Name);

                        if (bool.Parse(myReader["HasBackupToPermission"].ToString()))
                            userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].BackupToDatabaseServerList.Add(mainFrontEndSrver.Name);

                        if (bool.Parse(myReader["HasRestoreFromPermission"].ToString()))
                            userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].RestoreFromDatabaseServerList.Add(mainFrontEndSrver.Name);


                    }

				}

				object sync = new object();

				Parallel.ForEach(DatabaseServers.Instance.ItemsList.Where(item => item.IsPrimary == false), otherServer =>
				{
				   
					try
					{
						using (SqlConnection conn = new SqlConnection(otherServer.ConnectionString + ";Connection Timeout=30"))
						{
							SqlCommand command = conn.CreateCommand();
							command.CommandType = CommandType.Text;
							command.CommandText = @"SELECT " +
														"DomainUserName, " +
                                                        "HasCopyAndSearchFromPermission, " +
                                                        "HasBackupFromPermission, " +
                                                        "HasRestoreToPermission, " +
                                                        "HasBackupToPermission, " +
                                                        "HasRestoreFromPermission, " +
                                                        "GETUTCDATE() TimeStampDate " +
                                                    "FROM dbo.Users";

							conn.Open();
							SqlDataReader myReader = command.ExecuteReader(CommandBehavior.CloseConnection);
							while (myReader.Read())
							{
                                if (PermsTimeStamp > (DateTime)myReader["TimeStampDate"])
                                    PermsTimeStamp = (DateTime)myReader["TimeStampDate"];

                                if (!userSpecificPermissionsCandidate.ContainsKey(myReader["DomainUserName"].ToString().ToUpper()))
									continue;

                                if (bool.Parse(myReader["HasRestoreFromPermission"].ToString()))
                                {
                                    lock (sync)
                                    {
                                        userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].RestoreFromDatabaseServerList.Add(otherServer.Name);
                                    }
                                }

                                if (bool.Parse(myReader["HasCopyAndSearchFromPermission"].ToString()))
								{
									lock (sync)
									{
										userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].CopyAndSearchFromDatabaseSeverList.Add(otherServer.Name);
									}
								}

								if (bool.Parse(myReader["HasBackupFromPermission"].ToString()))
								{
									lock (sync)
									{
										userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].BackupFromDatabaseServerList.Add(otherServer.Name);
									}
								}

                                if (bool.Parse(myReader["HasRestoreToPermission"].ToString()))
                                {
                                    lock (sync)
                                    {
                                        userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].RestoreToServerList.Add(otherServer.Name);
                                    }
                                }

                                if (bool.Parse(myReader["HasBackupToPermission"].ToString()))
                                {
                                    lock (sync)
                                    {
                                        userSpecificPermissionsCandidate[myReader["DomainUserName"].ToString().ToUpper()].BackupToDatabaseServerList.Add(otherServer.Name);
                                    }
                                }

                            }
						}

					}
					catch (Exception ex)
					{
						Log.ErrorFormat("Exception while retrieving permissions from: {0}, {1}.", otherServer.Name, ex.Message);
					}
				});



				lock (lockObj)
				{
					_userSpecificPermissions = userSpecificPermissionsCandidate;
				}

                return true;

			}
			catch (Exception ex)
			{
				Log.Error("Failed to retrieve user list from database.", ex);
				return false;
			}


		}
	}
}