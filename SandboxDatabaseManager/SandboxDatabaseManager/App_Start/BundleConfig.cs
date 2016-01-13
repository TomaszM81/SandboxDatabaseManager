using System.Web;
using System.Web.Optimization;

namespace SandboxDatabaseManager
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                      "~/Scripts/jquery-{version}.js",
                      "~/Scripts/jquery.unobtrusive*"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js",
                      "~/Scripts/jquery.cookie.js",
                      "~/Scripts/jquery.signalR-2.2.0.js",
                      "~/Scripts/toggle.js",
                      "~/Scripts/performance-counters.js",
                      "~/Scripts/background-task-stats.js",
                      "~/Scripts/dygraph-combined.js",
                      "~/Scripts/jquery.datetimepicker.js",
                      "~/Scripts/jquery-dateFormat.min.js"
                      ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/simple-sidebar.css",
                      "~/Content/jquery.datetimepicker.css",
                      "~/Content/site.css"
                      ));

        }
    }
}
