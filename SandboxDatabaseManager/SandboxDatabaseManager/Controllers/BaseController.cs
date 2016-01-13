using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    public class BaseController : Controller
    {
        private log4net.ILog logger;

        protected override void OnException(ExceptionContext filterContext)
        {
            //Log error
            logger = log4net.LogManager.GetLogger(filterContext.Controller.ToString());
            logger.Error(filterContext.Exception.Message, filterContext.Exception);
          
        }
    }
}