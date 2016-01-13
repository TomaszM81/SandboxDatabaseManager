using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Tasks;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class SearchDataController : BaseController
    {
        
        public ActionResult GetSearchParams()
        {

            ViewBag.ServersWithBackupPermission = UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].BackupFromDatabaseServerList;
            ViewBag.DatabaseServerList = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).OrderBy(item => item.Name).Select(item => item.Name).Union(new string[] { "All Servers" }).ToList();
            ViewBag.PreselectedDatabaseServer = "All Servers";
            ViewBag.DatabaseSerachKey = null;


            return View();
        }

        public ActionResult Search(string databaseServerFilter, string sqlStatement, string databaseNameFilter = null)
        {
            IBgTask task = new SearchDataTask(databaseServerFilter, databaseNameFilter, sqlStatement, User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
        


        public ActionResult ShowSearchResults(string taskGuid)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();


            if (string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");

            ViewBag.UsedSearchSqlStatement = task.Description;

            return View((DataSet)task.Result);

        }

    }
}