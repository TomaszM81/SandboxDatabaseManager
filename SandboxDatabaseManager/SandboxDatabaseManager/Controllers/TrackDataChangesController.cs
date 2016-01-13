using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Models;
using SandboxDatabaseManager.Tasks;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    public class TrackDataChangesController : BaseController
    {
        [HttpPost]
        public ActionResult Track(ChangeTrackingDetails data)
        {
            ViewBag.ChangeTrackingEnabled = true;

            if (!data.ShowChangesFromRevision.HasValue)
            {
                if (!DatabaseContext.CheckIfTrackingEnabled(data.DatabaseServer, data.DatabaseName, User.Identity.Name).HasValue)
                {
                    // then this must come directly from Database Actions menu and I disable part of view
                    ViewBag.ChangeTrackingEnabled = false;
                }
            }
            else
            {
                List<string> sqlStatementList = new List<string>();

                var tableList = Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)];
                if(tableList != null)
                {
                    if(String.Compare((string)tableList, data.ListOfTablesToCompare, false) == 0)
                    {

                        sqlStatementList = Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] as List<string>;

                        if (sqlStatementList == null)
                        {
                            sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                            Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                            Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
                        }

                    }else
                    {
                        Session.Remove(String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName));
                        Session.Remove(String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName));

                        sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                        Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                        Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
                    }

                }
                else
                {
                    sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                    Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                    Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
                }


                string logMessage;
                data.TrackDataChangesReslut = DatabaseContext.TrackDataChanges(data.DatabaseServer, data.DatabaseName, User.Identity.Name, sqlStatementList, data.ShowChangesFromRevision, out logMessage);
                data.TrackDataChangesLog = logMessage;
            }

            return View(data);
        }


        public ActionResult GetTrackData(ChangeTrackingDetails data)
        {

            List<string> sqlStatementList = new List<string>();

            var tableList = Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)];
            if (tableList != null)
            {
                if (String.Compare((string)tableList, data.ListOfTablesToCompare, false) == 0)
                {

                    sqlStatementList = Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] as List<string>;

                    if (sqlStatementList == null)
                    {
                        sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                        Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                        Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
                    }

                }
                else
                {
                    Session.Remove(String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName));
                    Session.Remove(String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName));

                    sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                    Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                    Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
                }

            }
            else
            {
                sqlStatementList = DatabaseContext.GetTablesForDataTrack(data.DatabaseServer, data.DatabaseName, data.ListOfTablesToCompare, User.Identity.Name);

                Session[String.Format("TrackingTables{0}{1}", data.DatabaseServer, data.DatabaseName)] = data.ListOfTablesToCompare;
                Session[String.Format("TrackingStatements{0}{1}", data.DatabaseServer, data.DatabaseName)] = sqlStatementList;
            }


            string logMessage;
                data.TrackDataChangesReslut = DatabaseContext.TrackDataChanges(data.DatabaseServer, data.DatabaseName, User.Identity.Name, sqlStatementList, data.ShowChangesFromRevision, out logMessage);
                data.TrackDataChangesLog = logMessage;
            

            return PartialView("_TrackData", data);
        }

        [HttpGet]
        public ActionResult Track(string taskGuid)
        {

            var task = TaskContainer.Instance.GetTask(taskGuid);
            if (task == null)
                return new EmptyResult();


            if (string.Compare(task.Owner, User.Identity.Name, true) != 0)
                return Redirect("~/nopowerhere.html");

            ViewBag.ChangeTrackingEnabled = true;

            ChangeTrackingDetails data = (ChangeTrackingDetails)task.RedirectOnlyResult;

            if (!data.ShowChangesFromRevision.HasValue)
            {
                if (!DatabaseContext.CheckIfTrackingEnabled(data.DatabaseServer, data.DatabaseName, User.Identity.Name).HasValue)
                {
                    // then this must come directly from Databese Actions menu and I disable part of view
                    ViewBag.ChangeTrackingEnabled = false;
                }
            }

            return View(data);
        }

        public ActionResult GetLatestRevision(string databaseServer, string databaseName)
        {
            return Json(DatabaseContext.GetLatestRevisionChangeTracking(databaseServer, databaseName), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Enable(string databaseServer, string databaseName)
        {
            IBgTask task = new EnableChangeTrackingTask(databaseServer, databaseName, User.Identity.Name);

            Session.Remove(String.Format("TrackingStatements{0}{1}", databaseServer, databaseName));
            Session.Remove(String.Format("TrackingTables{0}{1}", databaseServer, databaseName));

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }

        public ActionResult Disable(string databaseServer, string databaseName)
        {
            IBgTask task = new DisableChangeTrackingTask(databaseServer, databaseName, User.Identity.Name);

            task.Start();
            TaskContainer.Instance.AddTask(task);

            return RedirectToAction("GetProgress", "Task", new { taskGuid = task.ID });
        }
    }
}