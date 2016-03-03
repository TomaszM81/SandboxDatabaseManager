using SandboxDatabaseManager.Tasks;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class TaskController : BaseController
    {
        // GET: Tast
        public ActionResult GetProgress(string taskGuid, bool noRedirect = false)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();


            if(string.Compare(task.Owner,User.Identity.Name,true) != 0)
                return Redirect("~/nopowerhere.html");

            // Sore the status here as it may change since now and time the view is rendered
            ViewBag.Status = task.Status;

            if (task.Status != TaskStatus.Running)
            {
                BackgroundTasksStatsSender.Instance.DiscardTasksFromReporting(task.Owner, new List<string>() { task.ID });
            }

            if ((TaskStatus)ViewBag.Status == TaskStatus.Succeeded && !String.IsNullOrWhiteSpace(task.RedirectToController) && !String.IsNullOrWhiteSpace(task.RedirectToAction) && !noRedirect)
                return RedirectToAction(task.RedirectToAction, task.RedirectToController, new { taskGuid = task.ID });

            return View(task);
        }

        [HttpPost]
        public ActionResult RequestAbort(string taskGuid)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();

            if (string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");

            return new EmptyResult();
        }

        public ActionResult GetTaskProgress(string taskGuid, int skipOutput)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();


            if (string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");


            if (task.Status != TaskStatus.Running)
            {
                BackgroundTasksStatsSender.Instance.DiscardTasksFromReporting(task.Owner, new List<string>() { task.ID });
            }

            return Json(new
            {
                status = task.Status == TaskStatus.Running ? "Running... " : task.Status.ToString(),
                output = task.OutputText.Substring(skipOutput > task.OutputText.Length ? task.OutputText.Length : skipOutput),
                duration = task.DurationString,
                redirectToController = task.RedirectToController,
                redirectToAction = task.RedirectToAction
            }, JsonRequestBehavior.AllowGet);



        }

        public ActionResult MyTasks(string status = "")
        {
            ViewBag.TaskSubMessage = "last";

            if (status != "")
            {
                ViewBag.TaskSubMessage = "recent new " + status + " ";
            }

            var tasks = TaskContainer.Instance.GetRecentTop15TasksForUser(User.Identity.Name, status);

            var itemsToDiscard = tasks.Where(task => !task.DiscardedFromStatsReporting && task.Status != TaskStatus.Running).ToList();
            itemsToDiscard.ForEach(item => item.DiscardedFromStatsReporting = true);
            BackgroundTasksStatsSender.Instance.DiscardTasksFromReporting(User.Identity.Name, itemsToDiscard.Select(task => task.ID).ToList());

            return View(tasks);
        }

        [HttpPost]
        public ActionResult RemoveTask(string taskGuid)
        {
            var task = TaskContainer.Instance.GetTask(taskGuid);

            if(task != null && string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");

            TaskContainer.Instance.RemoveTask(task);

            return RedirectToAction("MyTasks", "Task");
        }

    }
}
