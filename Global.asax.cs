using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.App_Start;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Syslog;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                Start();
            }
            catch (Exception ex)
            {
                var errorMessage = FlattenException(ex);
                StartupLogger.Error(ex, "Unable to start: {Message:l}", errorMessage);
            }
        }

        private void Start()
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
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.File(formatter, $"{path}\\Logs\\log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.File($"{path}\\Logs\\log-.txt", rollingInterval: RollingInterval.Day);
            }

            ConfigureSyslog(loggerConfiguration, out var syslogInfoMessage);
            Log.Logger = loggerConfiguration.CreateLogger();
            Serilog.Debugging.SelfLog.Enable(x => Debug.WriteLine(x));

            try
            {
                Configuration.Load();
                SetLogLevel(Configuration.Current.LogLevel, levelSwitch);

                if (syslogInfoMessage != null)
                {
                    Log.Logger.Debug(syslogInfoMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }

            var services = new ServiceCollection();

            services.AddSingleton(Log.Logger);
            services.AddSingleton(Configuration.Current);

            ServicesConfig.RegisterControllers(services);
            ServicesConfig.RegisterServices(services);

            var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

            DependencyResolver.SetResolver(new CustomDependencyResolver(provider));
            ControllerBuilder.Current.SetControllerFactory(new CustomControllerFactory(provider));

            RemoveSomeHeaders();
        }

        protected void Application_Error()
        {
            var logger = Log.Logger;
            var ex = Server.GetLastError();

            if (ex is HttpException httpException)
            {
                switch (httpException.GetHttpCode())
                {
                    case 401:
                        HandleUnauthError();
                        return;
                }
            }

            if (ex is UnauthorizedException)
            {
                HandleUnauthError();
                return;
            }

            if (ex is AccessForbiddenException)
            {
                HttpContext.Current.Server.ClearError();
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.Redirect("~/Error/AccessDenied");
                return;
            }
            
            if (ex is ForbiddenException)
            {
                HandleUnauthError();
                return;
            }

            if (ex is PasswordChangingSessionExpired pwdEx)
            {
                logger.Warning(ex, "Password changing session expired for user '{u:l}'", pwdEx.Identity);
                HandleUnauthError();
                return;
            }

            if (ex is HttpAntiForgeryException)
            {
                Server.ClearError();
                Response.Redirect("~/Error/SessionExpired");
                return;
            }

            if (ex is FeatureNotEnabledException featureEx)
            {
                var rd = HttpContext.Current.Request.RequestContext.RouteData;
                var action = rd.Values["action"] ?? "action";
                var controller = rd.Values["controller"] ?? "controller";
                var route = $"/{controller}/{action}".ToLower();
                logger.Warning("Unable to navigate to route '{r:l}' because required feature '{f:l}' is not enabled.",
                    route, featureEx.FeatureDescription);

                HttpContext.Current.Server.ClearError();
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.Redirect("~/home");

                return;
            }

            logger.Error(ex, "Unhandled error: {msg:l}", ex.Message);
            HttpContext.Current.Server.ClearError();
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Redirect("~/error");
        }

        private void HandleUnauthError()
        {
            HttpContext.Current.Server.ClearError();
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Redirect(FormsAuthentication.LoginUrl);
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

        //security requirements
        private void RemoveSomeHeaders()
        {
            //it removes the X-AspNetMvc-Version from the response header
            MvcHandler.DisableMvcResponseHeader = true;
        }

        private static void ConfigureSyslog(LoggerConfiguration loggerConfiguration, out string logMessage)
        {
            logMessage = null;

            var appSettings = Configuration.PortalSettings;
            if (appSettings == null)
            {
                return;
            }

            var sysLogServer = appSettings["syslog-server"];
            var sysLogFormatSetting = appSettings["syslog-format"];
            var sysLogFramerSetting = appSettings["syslog-framer"];
            var sysLogFacilitySetting = appSettings["syslog-facility"];
            var sysLogAppName = appSettings["syslog-app-name"] ?? "multifactor-portal";
            var sysLogTemplate = appSettings["syslog-template"];
            if (!bool.TryParse(appSettings["syslog-use-tls"], out var sysLogUseTls))
            {
                sysLogUseTls = true;
            }

            var isJson = Configuration.GetLogFormat() == "json";

            var facility = ParseSettingOrDefault(sysLogFacilitySetting, Facility.Auth);
            var format = ParseSettingOrDefault(sysLogFormatSetting, SyslogFormat.RFC5424);
            var framer = ParseSettingOrDefault(sysLogFramerSetting, FramingType.OCTET_COUNTING);

            if (sysLogServer != null)
            {
                var uri = new Uri(sysLogServer);

                if (uri.Port == -1)
                {
                    throw new ConfigurationErrorsException($"Invalid port number for syslog-server {sysLogServer}");
                }

                switch (uri.Scheme)
                {
                    case "udp":
                        var serverIp = ResolveIP(uri.Host);
                        loggerConfiguration
                            .WriteTo
                            .JsonUdpSyslog(serverIp,
                                port: uri.Port,
                                appName: sysLogAppName,
                                format: format,
                                outputTemplate: !string.IsNullOrWhiteSpace(sysLogTemplate) ? sysLogTemplate : null,
                                facility: facility,
                                json: isJson);
                        logMessage =
                            $"Using UDP syslog server: {sysLogServer}, format: {format}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    case "tcp":
                        loggerConfiguration
                            .WriteTo
                            .JsonTcpSyslog(uri.Host,
                                uri.Port,
                                certValidationCallback: ValidateServerCertificate,
                                secureProtocols: sysLogUseTls
                                    ? System.Security.Authentication.SslProtocols.Tls12
                                    : System.Security.Authentication.SslProtocols.None,
                                appName: sysLogAppName,
                                format: format,
                                outputTemplate: !string.IsNullOrWhiteSpace(sysLogTemplate) ? sysLogTemplate : null,
                                framingType: framer,
                                facility: facility,
                                json: isJson);
                        logMessage =
                            $"Using TCP syslog server {sysLogServer}, format: {format}, framing: {framer}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    default:
                        throw new NotImplementedException(
                            $"Unknown scheme {uri.Scheme} for syslog-server {sysLogServer}. Expected udp or tcp");
                }
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors) => true;

        private static TEnum ParseSettingOrDefault<TEnum>(string setting, TEnum defaultValue) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(setting, out var val))
            {
                return val;
            }

            return defaultValue;
        }

        private static string ResolveIP(string host)
        {
            if (!IPAddress.TryParse(host, out var addr))
            {
                addr = Dns.GetHostAddresses(host)
                    .First(x => x.AddressFamily == AddressFamily.InterNetwork); //only ipv4

                return addr.ToString();
            }

            return host;
        }

        private static ITextFormatter GetLogFormatter()
        {
            var format = Configuration.GetLogFormat();
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            var counter = 0;

            while (exception != null)
            {
                if (counter++ > 0)
                {
                    var prefix = new string('-', counter) + ">\t";
                    stringBuilder.Append(prefix);
                }

                stringBuilder.AppendLine(exception.Message);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}