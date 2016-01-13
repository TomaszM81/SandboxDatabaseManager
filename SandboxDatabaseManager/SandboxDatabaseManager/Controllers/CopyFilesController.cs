using SandboxDatabaseManager.Configuration;
using SandboxDatabaseManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{

    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class CopyFilesController : BaseController
    {

        [HttpGet, ValidateInput(false)]
        public ActionResult GetFileCopyDetails(FileCopyDetails model)
        {

            ModelState.Clear();

            ViewBag.CopyFileLocations = CopyFileLocations.Instance.ItemsList.Select(i => new SelectListItem() { Text = i.Name, Value = i.Path }).ToList();

            return View(model);
        }


        [HttpPost]
        public ActionResult Copy(FileCopyDetails model)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.CopyFileLocations = CopyFileLocations.Instance.ItemsList.Select(i => new SelectListItem() { Text = i.Name, Value = i.Path }).ToList();

                return View("GetFileCopyDetails", model);
            }

            return View();
        }
    }
}