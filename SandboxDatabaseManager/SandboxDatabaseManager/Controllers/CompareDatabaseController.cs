using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Models;
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
    public class CompareDatabaseController : BaseController
    {
        public ActionResult GetCompareDetails(string one = null, string two = null)
        {

            ViewBag.DatabaseServerList = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).OrderBy(item => item.Name).Select(item => item.Name).Union(new string[] { "All Servers" }).ToList();
            return View();
        }

        public ActionResult Compare(DatabaseCompareDetails data)
        {
            ViewBag.DatabaseServerList = DatabaseServers.Instance.ItemsList.Where(server => UserPermissions.Instance.UserSpecificPermissions[User.Identity.Name.ToUpper()].CopyAndSearchFromDatabaseSeverList.Contains(server.Name)).OrderBy(item => item.Name).Select(item => item.Name).Union(new string[] { "All Servers" }).ToList();
            ViewBag.PreSelectedItem = data.DatabaseServer;

            if (data.DatabaseNameToCompare == data.DatabaseNameToCompareAgaints)
                ModelState.AddModelError(String.Empty, "Select two different databases to compare.");

            if (!ModelState.IsValid)
            {
                return View("GetCompareDetails");
            }

            IBgTask task = new CompareDatabaseTask(data.DatabaseServer, data.DatabaseNameToCompare, data.DatabaseNameToCompareAgaints, data.ListOfTablesToCompare, data.MaxTableRowCount, User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }

        public ActionResult ShowCompareResult(string taskGuid)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();


            if (string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");


            ViewBag.Message = task.Name;

            return View((DataTable)task.Result);
        }
    }
}