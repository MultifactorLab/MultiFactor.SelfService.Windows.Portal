using System.Web.Optimization;

namespace MultiFactor.SelfService.Windows.Portal
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Content/js/jquery-{version}.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include("~/Content/js/jquery.validate*"));
            bundles.Add(new ScriptBundle("~/bundles/jquery.validate.unobtrusive").Include("~/Content/js/jquery.validate.unobtrusive.min.js"));
            bundles.Add(new StyleBundle("~/content/css").Include("~/Content/style/site.css"));
        }
    }
}
