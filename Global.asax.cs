using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MultiFactor.SelfService.Windows.Portal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            BundleTable.EnableOptimizations = true;

            var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            //create logging
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.File($"{path}\\Logs\\log-.txt", rollingInterval: RollingInterval.Day);

            Log.Logger = loggerConfiguration.CreateLogger();

            try
            {
                Configuration.Load();
                SetLogLevel(Configuration.Current.LogLevel, levelSwitch);
            }
            catch(Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }
        }

        private void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
        {
            switch (level)
            {
                case "Debug":
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Info":
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warn":
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
            }
        }
    }
}
