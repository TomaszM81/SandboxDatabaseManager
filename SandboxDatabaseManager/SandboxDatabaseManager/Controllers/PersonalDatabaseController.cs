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
using System.Web.UI.WebControls;
using System.Threading.Tasks;
using System.Data;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class PersonalDatabaseController : BaseController
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

                ViewBag.DatabaseServerList = DatabaseServers.Instance.ItemsList.OrderBy(item => item.Name).Select(item => item.Name).Union(new string[] { "All Servers" }).ToList();
                ViewBag.PreselectedDatabaseServer = databaseServerFilter;
                ViewBag.DatabaseSerachKey = databaseNameFilter;

                return DatabaseContext.ListDatabases(databaseServerFilter, ViewBag.DatabaseSerachKey, User.Identity.Name);
            });

            return View(result);
        }

        public async Task<ActionResult> GetDatabases(string databaseServerFilter = null, string databaseNameFilter = null)
        {
            DataSet result = await Task<DataSet>.Factory.StartNew(() =>
            {
                return DatabaseContext.ListDatabases(databaseServerFilter, databaseNameFilter, User.Identity.Name);
            });

            return PartialView("_Databases", result);
        }

       


        public ActionResult CreateSnapshotGetDetails(FormCollection data)
        {
            return View(new CreateSnapshotDetails() { DatabaseName = data["Database"], DatabaseServer = data["Server"]});
        }
        public ActionResult CreateSnapshot(CreateSnapshotDetails data)
        {


            if (ModelState.IsValid)
            {
                var result = DatabaseContext.ValidateTargetDatabaseSnaphotName(data.DatabaseServer, data.DatabaseSnapshotName, User.Identity.Name.ToUpper());
                if (!result.Item1)
                {
                    if (result.Item2)
                    {
                        ViewBag.WarningMessage = String.Format("Warning ! You already have a snapshot named [{0}] on server [{1}], please confirm if you wish to overwrite it.", data.DatabaseSnapshotName, data.DatabaseServer);
                        if (data.LastWarningMessage != (string)ViewBag.WarningMessage)
                        {
                            data.LastWarningMessage = (string)ViewBag.WarningMessage;
                            return View("CreateSnapshotGetDetails", data);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("DatabaseSnapshotName ", "A snapshot or database with specified Database Snapshot Name already exists, please provide a different name.");
                        return View("CreateSnapshotGetDetails", data);
                    }
                }
            }
            else
            {
                data.LastWarningMessage = null;
                return View("CreateSnapshotGetDetails", data);
            }


            IBgTask task = new CreateDatabaseSnapshotTask(data.DatabaseServer, data.DatabaseName, data.DatabaseSnapshotName, User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
        public ActionResult RevertToDatabaseSnapshotConfirm(FormCollection data)
        {
            ViewBag.ConfirmMessage = String.Format("Are you sure you want to revert {0} database on {1} server to {2} database snapshot ?" + Environment.NewLine +
                                     "All remaining database snapshot for this database will be removed.", "<strong>"+ data["Database"] +"</strong>", "<strong>"+ data["Server"] +"</strong>", "<strong>"+ data["DatabaseSnapshot"] +"</strong>");

            ViewBag.YesRedirectController = "PersonalDatabase";
            ViewBag.YesRedirectAction = "RevertToDatabaseSnapshot";

            ViewBag.NoRedirectController = "PersonalDatabase";
            ViewBag.NoRedirectAction = "Index";


            return View("Confirm", data);

        }
        public ActionResult RevertToDatabaseSnapshot(FormCollection data)
        {
            IBgTask task = new RevertToDatabaseSnapshotTask(data["Server"], data["Database"], data["DatabaseSnapshot"], User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
        public ActionResult DropDatabaseSnapshotConfirm(FormCollection data)
        {
            ViewBag.ConfirmMessage = String.Format("Are you sure you want to drop {1} database snapshot on {0} server ?","<strong>" + data["Server"] + "</strong>", "<strong>" + data["DatabaseSnapshot"] + "</strong>");

            ViewBag.YesRedirectController = "PersonalDatabase";
            ViewBag.YesRedirectAction = "DropDatabaseSnapshot";

            ViewBag.NoRedirectController = "PersonalDatabase";
            ViewBag.NoRedirectAction = "Index";


            return View("Confirm", data);

        }
        public ActionResult DropDatabaseSnapshot(FormCollection data)
        {
            IBgTask task = new DropDatabaseSnapshotTask(data["Server"], data["DatabaseSnapshot"], User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
        public ActionResult DropDatabaseConfirm(FormCollection data)
        {
            ViewBag.ConfirmMessage = String.Format("Are you sure you want to drop {1} database on {0} server ?", "<strong>" + data["Server"] + "</strong>", "<strong>" + data["Database"] + "</strong>");

            ViewBag.YesRedirectController = "PersonalDatabase";
            ViewBag.YesRedirectAction = "DropDatabase";

            ViewBag.NoRedirectController = "PersonalDatabase";
            ViewBag.NoRedirectAction = "Index";


            return View("Confirm", data);

        }
        public ActionResult DropDatabase(FormCollection data)
        {
            IBgTask task = new DropDatabaseTask(data["Server"], data["Database"], User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
        public ActionResult EditCommentGetDetails(FormCollection data)
        {
            EditCommentData model = new EditCommentData() { DatabaseComment = data["DatabaseComment"], DatabaseServer = data["Server"], DatabaseName = data["Database"] };

            return View(model);
        }
        public ActionResult EditComment(EditCommentData model)
        {
            DatabaseContext.ChangeDatabaseComment(model.DatabaseServer, model.DatabaseName, User.Identity.Name, model.DatabaseComment);

            return RedirectToAction("Index");
        }
        public ActionResult TransferOwnershipGetDetails(FormCollection data)
        {
            TransferOwnershipData model = new TransferOwnershipData() { DatabaseServer = data["Server"], DatabaseName = data["Database"] };

            List<string> userList = new List<string>();
            foreach(var user in UserPermissions.Instance.UserSpecificPermissions)
            {
                if (user.Value.RestoreToServerList.Contains(model.DatabaseServer))
                    userList.Add(user.Key);
            }

            ViewBag.UserList = userList;


            return View(model);
        }
        public ActionResult TransferOwnership(TransferOwnershipData model)
        {
            DatabaseContext.TransferDatabaseToUser(model.DatabaseServer, model.DatabaseName, User.Identity.Name, model.NewDatabaseOwner);

            return RedirectToAction("Index");
        }
        public ActionResult KillConnectionsToDatabaseConfirm(FormCollection data)
        {
            ViewBag.ConfirmMessage = String.Format("Are you sure you want to kill all connections to {1} database on {0} server ?", "<strong>" + data["Server"] + "</strong>", "<strong>" + data["Database"] + "</strong>");

            ViewBag.YesRedirectController = "PersonalDatabase";
            ViewBag.YesRedirectAction = "KillConnectionsToDatabase";

            ViewBag.NoRedirectController = "PersonalDatabase";
            ViewBag.NoRedirectAction = "Index";


            return View("Confirm", data);

        }
        public ActionResult KillConnectionsToDatabase(FormCollection data)
        {
            DatabaseContext.KillConnectionsToPersonalDatabase(data["Server"], data["Database"], User.Identity.Name);

            return RedirectToAction("Index");
        }
        


    }


     
}