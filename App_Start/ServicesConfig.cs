using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex;
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
using System.Net;
using System.Net.Http;

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
            var clientConf = services.AddTransient<YandexCaptchaApi>()
                .AddTransient<YandexHttpClientAdapterFactory>()
                .AddHttpClient<YandexHttpClientAdapterFactory>((client) =>
                {
                    client.BaseAddress = new Uri("https://captcha-api.yandex.ru/");
                });

            ConfigureCaptchaProxyCredentials(clientConf);
        }

        private static void ConfigureGoogleApi(ServiceCollection services)
        {
            var clientConf = services.AddTransient<GoogleReCaptcha2Api>()
                .AddTransient<GoogleCaptchaHttpClientAdapterFactory>()
                .AddHttpClient<GoogleCaptchaHttpClientAdapterFactory>((x) =>
                {
                    x.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
                });

            ConfigureCaptchaProxyCredentials(clientConf);
        }

        private static void ConfigureCaptchaProxyCredentials(IHttpClientBuilder clientConf)
        {
            if (!string.IsNullOrEmpty(Configuration.Current.CaptchaProxy))
            {
                var proxyUri = new Uri(Configuration.Current.CaptchaProxy);
                var proxy = new WebProxy(proxyUri);
                if (!string.IsNullOrEmpty(proxyUri.UserInfo))
                {
                    var credentials = proxyUri.UserInfo.Split(new[] { ':' }, 2);
                    proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
                }
                clientConf.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    Proxy = proxy
                });
            }
        }

    }
}