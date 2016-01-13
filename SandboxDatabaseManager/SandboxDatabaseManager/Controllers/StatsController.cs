using SandboxDatabaseManager.Database;
using SandboxDatabaseManager.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SandboxDatabaseManager.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.Disabled)]
    public class StatsController : BaseController
    {
        // GET: Stats
        public ActionResult GetStats(string serverName, string counterName, string chartYAxisSufix, DateTime? startDate, DateTime? endDate)
        {
            if (endDate.HasValue && endDate.Value > DateTime.Now)
                endDate = null;

            ViewBag.endDateSpecified = endDate.HasValue;

            if (startDate.HasValue || endDate.HasValue)
            {
                ViewBag.CoutersData = DatabaseContext.GetCountersData(serverName, counterName, startDate, endDate);
                ViewBag.startDate = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd HH:mm") : "∞";
                ViewBag.endDate = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd HH:mm") : "∞";
                ViewBag.customRange = true;

                if(!endDate.HasValue)
                {
                    var result = ((System.Data.DataTable)ViewBag.CoutersData).Compute("Max(CounterValueDate)", "") as DateTime?;
                    if (result.HasValue)
                    {
                        foreach(var dataRow in MonitoringBackgroundWorker.Instance.GetCounters(serverName, counterName).Select("CounterValueDate > '" + result.Value.ToString() + "'", "CounterValueDate asc"))
                        {
                            ((System.Data.DataTable)ViewBag.CoutersData).ImportRow(dataRow);
                        }
                    }
                          
                }

            }
            else
            {
                ViewBag.CoutersData = MonitoringBackgroundWorker.Instance.GetCounters(serverName, counterName);
                ViewBag.customRange = false;
            }

            ViewBag.CounterDataMinDate = MonitoringBackgroundWorker.Instance.GetCounterDataMinDate(serverName, counterName).ToString(@"yyyy\/MM\/dd");
            ViewBag.ServerName = serverName;
            ViewBag.CounterName = counterName;
            ViewBag.ChartYAxisSufix = chartYAxisSufix;



            return View();
        }
    }
}