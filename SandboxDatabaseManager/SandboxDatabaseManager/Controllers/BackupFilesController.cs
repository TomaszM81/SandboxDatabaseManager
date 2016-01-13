using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Models;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class BackupFilesController : BaseController
    {
        public async Task<ActionResult> Index(string locationNameFilter = null, string fileNameFilter = null, bool includeFull = true, bool includeDiff = false, bool includeLog = false)
        {
            BackupFilesResult result = await Task<BackupFilesResult>.Factory.StartNew(() =>
            {

                ViewBag.BackupFileLocationList = GetBackupFileLocationsList();
                ViewBag.PreselectedBackupFileLocation = locationNameFilter;
                ViewBag.FileNameFilter = fileNameFilter;

                return DatabaseContext.ListBackupFiles(User.Identity.Name.ToUpper(), locationNameFilter, fileNameFilter, includeFull, includeDiff, includeLog);
            });

            return View(result);
        }

        public async Task<ActionResult> GetBackupFiles(string locationNameFilter = null, string fileNameFilter = null, bool includeFull = true, bool includeDiff = false, bool includeLog = false)
        {
            BackupFilesResult result = await Task<BackupFilesResult>.Factory.StartNew(() =>
            {
                return DatabaseContext.ListBackupFiles(User.Identity.Name.ToUpper(), locationNameFilter, fileNameFilter, includeFull, includeDiff, includeLog);
            });

            return PartialView("_BackupFiles", result);
        }


        private List<string> GetBackupFileLocationsList()
        {

            List<string> fileLocations = new List<string>();


            foreach (var server in DatabaseServers.Instance.ItemsList.Where(server => !String.IsNullOrWhiteSpace(server.BackupDatabaseNetworkSharePath) && UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].RestoreFromDatabaseServerList.Contains(server.Name)))
            {
                fileLocations.Add(server.Name);
            }

            foreach (var location in DatabaseBackupFileLocations.Instance.ItemsList)
            {
                fileLocations.Add(location.Name);
            }

            fileLocations.Sort();

            fileLocations.Add("All locations");



            return fileLocations;
        }
    }
}