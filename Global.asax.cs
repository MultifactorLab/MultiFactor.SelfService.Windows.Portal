﻿using Microsoft.Extensions.DependencyInjection;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MultiFactor.SelfService.Windows.Portal
{
    public class MvcApplication : HttpApplication
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

            try
            {
                Configuration.Load();
                SetLogLevel(Configuration.Current.LogLevel, levelSwitch);

                if (syslogInfoMessage != null)
                {
                    Log.Logger.Debug(syslogInfoMessage);
                }
            }
            catch(Exception ex)
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
        }

        protected void Application_Error()
        {
            var logger = Log.Logger;
            var ex = Server.GetLastError();

            if (ex is UnauthorizedException)
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
            
            if (ex is FeatureNotEnabledException featureEx)
            {
                var rd = HttpContext.Current.Request.RequestContext.RouteData;
                var action = rd.GetRequiredString("action");
                var controller = rd.GetRequiredString("controller");
                var route = $"/{controller}/{action}".ToLower();
                logger.Warning(ex, "Unable to navigate to route '{r:l}' because required feature '{f:l}' is not enabled.", route, featureEx.FeatureDescription);

                HttpContext.Current.Server.ClearError();
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.Redirect("~/home");

                return;
            }

            logger.Error(ex, "Unhandled error: {msg:l}", ex.Message);
        }

        private void HandleUnauthError()
        {
            HttpContext.Current.Server.ClearError();
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
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
                            .JsonUdpSyslog(serverIp, port: uri.Port, appName: sysLogAppName, format: format, facility: facility, json: isJson);
                        logMessage = $"Using syslog server: {sysLogServer}, format: {format}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    case "tcp":
                        loggerConfiguration
                            .WriteTo
                            .JsonTcpSyslog(uri.Host, uri.Port, appName: sysLogAppName, format: format, framingType: framer, facility: facility, json: isJson);
                        logMessage = $"Using syslog server {sysLogServer}, format: {format}, framing: {framer}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    default:
                        throw new NotImplementedException($"Unknown scheme {uri.Scheme} for syslog-server {sysLogServer}. Expected udp or tcp");
                }
            }
        }

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

    }
}