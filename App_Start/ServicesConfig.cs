using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex;
using MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Controllers;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Captcha;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using System;
using System.Reflection;

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
            services.AddTransient<ForgottenPasswordController>();
            services.AddTransient<ErrorController>();
        }

        internal static void RegisterServices(ServiceCollection services)
        {
            services.AddSingleton<IJsonDataSerializer, NewtonsoftJsonDataSerializer>();

            ConfigureGoogleApi(services);
            ConfigureYandexCaptchaApi(services);
            ConfigureCaptchaVerifier(services);

            services.AddScoped<JwtTokenProvider>();
            services.AddSingleton<ApiClient>();
            services.AddScoped<MultiFactorSelfServiceApiClient>();

            services.AddSingleton<TokenValidationService>();
            services.AddSingleton<ActiveDirectoryService>();
            services.AddSingleton<DataProtectionService>();
            services.AddSingleton<MultiFactorApiClient>();
            services.AddSingleton<AuthService>();
            services.AddPasswordChangingSessionCache();
        }


        private static void ConfigureGoogleApi(ServiceCollection services)
        {
            services.AddTransient<GoogleReCaptcha2Api>()
                .AddTransient<GoogleCaptchaHttpClientAdapterFactory>()
                .AddHttpClient<GoogleCaptchaHttpClientAdapterFactory>((x) =>
                {
                    x.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
                });
        }

        private static void ConfigureCaptchaVerifier(ServiceCollection services)
        {
            services.AddTransient<GoogleReCaptchaVerifier>();
            services.AddTransient<YandexCaptchaVerifier>();

            services.AddTransient<CaptchaVerifierResolver>(srv => () =>
            {
                if (Configuration.Current.IsCaptchaEnabled(CaptchaType.Yandex))
                {
                    return srv.GetRequiredService<YandexCaptchaVerifier>();
                }

                return srv.GetRequiredService<GoogleReCaptchaVerifier>();
            });
        }

        private static void ConfigureYandexCaptchaApi(ServiceCollection services)
        {
            services.AddTransient<YandexCaptchaApi>()
                .AddTransient<YandexHttpClientAdapterFactory>()
                .AddHttpClient<YandexHttpClientAdapterFactory>((client) =>
                {
                    client.BaseAddress = new Uri("https://captcha-api.yandex.ru/");
                });
        }

    }
}