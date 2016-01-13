using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class AboutController : BaseController
    {
        // GET: About
        public ActionResult About()
        {
            return View();
        }

        public ActionResult KeepAlive()
        {
            return new EmptyResult();
        }
    }
}