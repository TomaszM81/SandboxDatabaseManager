using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Models;
using SandboxDatabaseManager.Worker;
using SandboxDatabaseManager.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Threading.Tasks;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class DatabaseController : BaseController
    {
        public async Task<ActionResult> Index(string databaseServerFilter = null, string databaseNameFilter = null)
        {
            DataSet result = await Task<DataSet>.Factory.StartNew(() =>
            {
                databaseServerFilter = databaseServerFilter ?? DatabaseServers.Instance.ItemsList.First(item => item.IsPrimary).Name;


                if (UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].BackupToDatabaseServerList.Count == 0)
                {
                    // There is nowhere to backup this database to so the list of permissions will be empty at best
                    ViewBag.ServersWithBackupPermission = new List<string>();
                }
                else
                {
                    ViewBag.ServersWithBackupPermission = UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].BackupFromDatabaseServerList;
                }

                ViewBag.DatabaseServerList = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).OrderBy(item => item.Name).Select(item => item.Name).Union(new string[] { "All Servers" }).ToList();
                ViewBag.PreselectedDatabaseServer = databaseServerFilter;
                ViewBag.DatabaseSerachKey = databaseNameFilter;

                return DatabaseContext.ListDatabases(databaseServerFilter, databaseNameFilter, null, User.Identity.Name.ToUpper());
            });

            return View(result);
        }

        public async Task<ActionResult> GetDatabases(string databaseServerFilter = null, string databaseNameFilter = null)
        {
            DataSet result = await Task<DataSet>.Factory.StartNew(() =>
            {
                return DatabaseContext.ListDatabases(databaseServerFilter, databaseNameFilter, null, User.Identity.Name.ToUpper());
            });

            return PartialView("_Databases",result);
        }


        [HttpPost]
        public ActionResult CopyGetDetails(FormCollection data)
        {
            List<SelectListItem> list = GetRestorePermissionServerList();
            ViewBag.RestorePermissionServerList = list;
            ViewBag.ServersWithDefaultRecoveryToSimple = DatabaseServerSettings.Instance.DatabaseServerSettingsMap.Where(server => (bool)server.Value.GetSetting(DatabaseServerSettings.Setting.SetRecoveryModelToSimpleDefault)).Select(server => server.Key).ToList();

            bool preselectedDatabaseServerChangeRecoveryToSimple = false;
            if (list.Count > 0)
            {
                preselectedDatabaseServerChangeRecoveryToSimple = (ViewBag.ServersWithDefaultRecoveryToSimple as List<string>).Contains(list.First().Value) ? true : false;
            }

            return View(new CopyDatabaseDetails() { SourceDatabaseName = data["Database"], SourceDatabaseServer = data["Server"], SourceDatabaseSizeGB = decimal.Parse(data["DatabaseSize"]), SourceDatabaseRecoveryModel = data["RecoveryModel"], RecoveryModelChangeToSimple = preselectedDatabaseServerChangeRecoveryToSimple});
        }

        [HttpPost]
        public ActionResult Copy(CopyDatabaseDetails data)
        {

            ViewBag.RestorePermissionServerList = GetRestorePermissionServerList();
            ViewBag.ServersWithDefaultRecoveryToSimple = DatabaseServerSettings.Instance.DatabaseServerSettingsMap.Where(server => (bool)server.Value.GetSetting(DatabaseServerSettings.Setting.SetRecoveryModelToSimpleDefault)).Select(server => server.Key).ToList();

            if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(data.SourceDatabaseServer))
            {
                throw new Exception("You are unable to copy from this server.");
            }

            if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].RestoreToServerList.Contains(data.TargetDatabaseServer))
            {
                throw new Exception("You are unable to restore on this server.");
            }

            if (ModelState.IsValid)
            {
                var result = DatabaseContext.ValidateRestoreOperation(data.TargetDatabaseServer, data.TargetDatabaseName, HttpContext.Request.LogonUserIdentity.Name.ToUpper());
                if (result.CanOverwrite.HasValue)
                {
                    if (result.CanOverwrite.Value)
                    {
                        ViewBag.WarningMessage = String.Format("Warning ! You already have a database/snapshot named [{0}] on server [{1}], please confirm if you wish to overwrite it.", data.TargetDatabaseName, data.TargetDatabaseServer);
                        if (data.LastWarningMessage != (string)ViewBag.WarningMessage)
                        {
                            data.LastWarningMessage = (string)ViewBag.WarningMessage;
                            return View("CopyGetDetails", data);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("TargetDatabaseName ", "A database with specified Target Database Name already exists, please provide a different name.");
                        return View("CopyGetDetails", data);
                    }
                }

                if (!String.IsNullOrEmpty(result.CustomErrorMessage))
                {
                    ModelState.AddModelError("TargetDatabaseName ", result.CustomErrorMessage);
                    return View("CopyGetDetails", data);
                }


            }
            else
            {
                data.LastWarningMessage = null;
                return View("CopyGetDetails", data);
            }


            IBgTask task = new CopyDatabaseTask(data.SourceDatabaseServer, data.SourceDatabaseName, data.TargetDatabaseServer, data.TargetDatabaseName, data.DatabaseComment, data.RecoveryModelChangeToSimple, User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult BackupGetDetails(FormCollection data)
        {

            var model = new BackupDatabaseDetails() { SourceDatabaseName = data["Database"], SourceDatabaseServer = data["Server"], SourceDatabaseSizeGB = decimal.Parse(data["DatabaseSize"]) };

            ViewBag.RestorePermissionServerList = GetBackupToPermissionServerList();
            model.BackupServer = ((List<SelectListItem>)ViewBag.RestorePermissionServerList).Count > 0 ? ((List<SelectListItem>)ViewBag.RestorePermissionServerList)[0].Text: null;
            model.BackupDestinationPath = ((List<SelectListItem>)ViewBag.RestorePermissionServerList).Count > 0 ? ((dynamic)JValue.Parse(((List<SelectListItem>)ViewBag.RestorePermissionServerList)[0].Value)).BackupDatabaseNetworkSharePath : null;
            ViewBag.HasOverwriteBackupDestinationPermission = new Nullable<bool>(UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].HasOverwriteBackupDestinationPermission);

            return View(model);
        }

        public ActionResult Backup(BackupDatabaseDetails data)
        {

           

            if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].BackupFromDatabaseServerList.Contains(data.SourceDatabaseServer))
            {
                throw new Exception("You are unable to take backups from this server.");
            }




            if (data.IsOverride.HasValue && data.IsOverride.Value)
            {
                if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].HasOverwriteBackupDestinationPermission)
                {
                    throw new Exception("You do not have OverwriteBackupDestinationPermission.");
                }
            }else
            {
                dynamic serverObject = JValue.Parse(data.BackupServer);
                if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].BackupToDatabaseServerList.Contains((string)serverObject.BackupDatabaseServer))
                    throw new Exception("You are unable to backup onto this server.");

                data.BackupDestinationPath = DatabaseServers.Instance.ItemsList.First(server => server.Name == (string)serverObject.BackupDatabaseServer).BackupDatabaseNetworkSharePath;

            }

            if (!ModelState.IsValid)
            {
                ViewBag.RestorePermissionServerList = GetBackupToPermissionServerList();
                ViewBag.HasOverwriteBackupDestinationPermission = new Nullable<bool>(UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].HasOverwriteBackupDestinationPermission);
                return View("BackupGetDetails", data);
            }


            IBgTask task = new BackupDatabaseTask(data.SourceDatabaseServer, data.SourceDatabaseName, User.Identity.Name, data.BackupComment,Path.Combine(data.BackupDestinationPath, data.BackupFileName));

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });




        }


        [HttpPost, ValidateInput(false)]
        public ActionResult RestoreGetDetails(RestoreDatabaseModel data)
        {
            List<SelectListItem> list = GetRestorePermissionServerList();
            ViewBag.RestorePermissionServerList = list;
            ViewBag.ServersWithDefaultRecoveryToSimple = DatabaseServerSettings.Instance.DatabaseServerSettingsMap.Where(server => (bool)server.Value.GetSetting(DatabaseServerSettings.Setting.SetRecoveryModelToSimpleDefault)).Select(server => server.Key).ToList();

            bool preselectedDatabaseServerChangeRecoveryToSimple = false;
            if (list.Count > 0)
            {
                preselectedDatabaseServerChangeRecoveryToSimple = (ViewBag.ServersWithDefaultRecoveryToSimple as List<string>).Contains(list.First().Value) ? true : false;
            }

            data.RecoveryModelChangeToSimple = preselectedDatabaseServerChangeRecoveryToSimple;
            data.RestoreWithRecovery = true;

            //clear any errors
            ModelState.Clear();
            return View(data);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Restore(RestoreDatabaseModel data)
        {


            ViewBag.RestorePermissionServerList = GetRestorePermissionServerList();
            ViewBag.ServersWithDefaultRecoveryToSimple = DatabaseServerSettings.Instance.DatabaseServerSettingsMap.Where(server => (bool)server.Value.GetSetting(DatabaseServerSettings.Setting.SetRecoveryModelToSimpleDefault)).Select(server => server.Key).ToList();


            if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].RestoreToServerList.Contains(data.TargetDatabaseServer))
            {
                throw new Exception("You are unable to restore on this server.");
            }

            if (ModelState.IsValid)
            {
                var result = DatabaseContext.ValidateRestoreOperation(data.TargetDatabaseServer, data.TargetDatabaseName, HttpContext.Request.LogonUserIdentity.Name.ToUpper(), data.BackupType, decimal.Parse(data.FirstLSN), decimal.Parse(data.LastLSN), decimal.Parse(data.DatabaseBackupLSN));
                if (result.CanOverwrite.HasValue)
                {
                    if (result.CanOverwrite.Value)
                    {
                        ViewBag.WarningMessage = String.Format("Warning ! You already have a database/snapshot named [{0}] on server [{1}], please confirm if you wish to overwrite it.", data.TargetDatabaseName, data.TargetDatabaseServer);
                        if (data.LastWarningMessage != (string)ViewBag.WarningMessage)
                        {
                            data.LastWarningMessage = (string)ViewBag.WarningMessage;
                            return View("RestoreGetDetails", data);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("TargetDatabaseName ", "A database with specified Target Database Name already exists, please provide a different name.");
                        return View("RestoreGetDetails", data);
                    }
                }

                if (!String.IsNullOrEmpty(result.CustomErrorMessage))
                {
                    ModelState.AddModelError("TargetDatabaseName ", result.CustomErrorMessage);
                    return View("RestoreGetDetails", data);
                }
            }
            else
            {
                data.LastWarningMessage = null;
                return View("RestoreGetDetails", data);
            }


            IBgTask task = new RestoreDatabaseTask(data.TargetDatabaseServer, data.TargetDatabaseName, User.Identity.Name, data.BackupServerName, data.BackupDatabaseName, data.DatabaseComment, data.BackupFileList, data.RecoveryModelChangeToSimple, data.PositinInFileCollection, data.BackupType, data.RestoreWithRecovery);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });

        }

        private List<SelectListItem> GetRestorePermissionServerList()
        {

            List<SelectListItem> restorePermissionServerList = new List<SelectListItem>();
            foreach (var serverData in DatabaseServers.Instance.ItemsList.OrderBy(item => item.Name))
            {

                if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].RestoreToServerList.Contains(serverData.Name))
                    continue;


                if (!String.IsNullOrWhiteSpace(serverData.MonitoredServerFriendlyNameForFreeSpace) && !String.IsNullOrWhiteSpace(serverData.MonitoredServerCounterFriendlyNameForFreeSpace))
                {
                    var serverCounterData = MonitoringBackgroundWorker.Instance.CurrentStats.FirstOrDefault(counterData => counterData.ServerName == serverData.MonitoredServerFriendlyNameForFreeSpace);
                    if (serverCounterData != null)
                    {
                        var counterData = serverCounterData.ListOfCounterValues.FirstOrDefault(counter => counter.CounterFriendlyName == serverData.MonitoredServerCounterFriendlyNameForFreeSpace);
                        if (counterData != null)
                        {
                            restorePermissionServerList.Add(new SelectListItem() { Value = serverData.Name, Text = String.Format("{0}  ({1} GB free)", serverData.Name, counterData.CounteValue.ToString("F0")) });
                            continue;
                        }
                    }
                }

                restorePermissionServerList.Add(new SelectListItem() { Value = serverData.Name, Text = serverData.Name });
            }


            return restorePermissionServerList;
        }

        private List<SelectListItem> GetBackupToPermissionServerList()
        {

            List<SelectListItem> restorePermissionServerList = new List<SelectListItem>();
            foreach (var serverData in DatabaseServers.Instance.ItemsList.OrderBy(item => item.Name))
            {
                if (!UserPermissions.Instance.UserSpecificPermissions[HttpContext.Request.LogonUserIdentity.Name.ToUpper()].BackupToDatabaseServerList.Contains(serverData.Name))
                    continue;

                // skip this item if the provided backup file path is empty
                if (String.IsNullOrWhiteSpace(serverData.BackupDatabaseNetworkSharePath))
                    continue;

                if (!String.IsNullOrWhiteSpace(serverData.MonitoredServerFriendlyNameForFreeSpace) && !String.IsNullOrWhiteSpace(serverData.MonitoredServerCounterFriendlyNameForFreeSpace))
                {
                    var serverCounterData = MonitoringBackgroundWorker.Instance.CurrentStats.FirstOrDefault(counterData => counterData.ServerName == serverData.MonitoredServerFriendlyNameForFreeSpace);
                    if (serverCounterData != null)
                    {
                        var counterData = serverCounterData.ListOfCounterValues.FirstOrDefault(counter => counter.CounterFriendlyName == serverData.MonitoredServerCounterFriendlyNameForFreeSpace);
                        if (counterData != null)
                        {
                            restorePermissionServerList.Add(new SelectListItem() { Value = JObject.FromObject(new { BackupDatabaseNetworkSharePath = serverData.BackupDatabaseNetworkSharePath, BackupDatabaseServer = serverData.Name }).ToString(), Text = String.Format("{0}  ({1} GB free)", serverData.Name, counterData.CounteValue.ToString("F0")) });
                            continue;
                        }
                    }
                }

                restorePermissionServerList.Add(new SelectListItem() { Value = JObject.FromObject(new { BackupDatabaseNetworkSharePath = serverData.BackupDatabaseNetworkSharePath, BackupDatabaseServer = serverData.Name }).ToString(), Text = serverData.Name });
            }

            return restorePermissionServerList;
        }
    }

}
