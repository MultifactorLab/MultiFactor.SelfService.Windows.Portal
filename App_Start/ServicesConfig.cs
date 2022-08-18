using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Controllers;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using System;

namespace MultiFactor.SelfService.Windows.Portal.App_Start
{
    public static class ServicesConfig
    {
        public static void RegisterControllers(ServiceCollection services)
        {
            services.AddTransient<AccountController>();
            services.AddTransient<ExchangeActiveSyncDevicesController>();
            services.AddTransient<ExpiredPasswordController>();
            services.AddTransient<HomeController>();
            services.AddTransient<MobileAppController>();
            services.AddTransient<PasswordController>();
            services.AddTransient<TelegramController>();
            services.AddTransient<TotpController>();
        }

        internal static void RegisterServices(ServiceCollection services)
        {
            services.AddSingleton<IJsonDataSerializer, NewtonsoftJsonDataSerializer>();

            services.AddTransient<ICaptchaVerifier, GoogleReCaptchaVerifier>();
            services.AddTransient<GoogleReCaptcha2Api>();
            services.AddTransient<GoogleCaptchaHttpClientAdapterFactory>();
            services.AddHttpClient<GoogleCaptchaHttpClientAdapterFactory>(x =>
            {
                x.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
            });
        }
    }
}