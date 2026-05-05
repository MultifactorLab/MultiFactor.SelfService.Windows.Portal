using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.Integrations.Captcha.Yandex;
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
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Options;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Windows.Portal.Stories.ChangeExpiredPassword;
using MultiFactor.SelfService.Windows.Portal.Stories.ChangeValidPassword;
using MultiFactor.SelfService.Windows.Portal.Stories.CheckExpiredPasswordSession;
using MultiFactor.SelfService.Windows.Portal.Stories.LoadProfile;
using MultiFactor.SelfService.Windows.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Windows.Portal.Stories.RecoverPassword;
using MultiFactor.SelfService.Windows.Portal.Stories.SearchExchangeActiveSyncDevices;
using MultiFactor.SelfService.Windows.Portal.Stories.SignIn;
using MultiFactor.SelfService.Windows.Portal.Stories.SignOut;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Stories.SignIn.ClaimsSources;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Windows.Portal.Core.Metadata;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims;

namespace MultiFactor.SelfService.Windows.Portal.App_Start
{
    public static class ServicesConfig
    {
        public static void RegisterControllers(ServiceCollection services)
        {
            services.AddTransient<AccountController>();
            services.AddTransient<ExchangeActiveSyncDevicesController>();
            services.AddTransient<Configure2FaController>();
            services.AddTransient<ExpiredPasswordController>();
            services.AddTransient<HomeController>();
            services.AddTransient<PasswordController>();
            services.AddTransient<ForgottenPasswordController>();
            services.AddTransient<UnlockController>();
            services.AddTransient<ErrorController>();
            services.AddTransient<LdapConnectionFactory>();
        }

        internal static void RegisterServices(ServiceCollection services)
        {
            services.AddSingleton<IJsonDataSerializer, NewtonsoftJsonDataSerializer>();
            ConfigureHttpClients(services);
            ConfigureApplicationSerivces(services);
            ConfigureGoogleApi(services);
            ConfigureYandexCaptchaApi(services);
            ConfigureCaptchaVerifier(services);
            ConfigureCloudConfiguration(services);
            ConfigureStories(services);

            services.AddScoped<JwtTokenProvider>();
            services.AddSingleton<ApiClient>();
            services.AddScoped<MultiFactorSelfServiceApiClient>();
            services.AddTransient<ScopeInfoService>();

            services.AddSingleton<TokenValidationService>();
            services.AddSingleton<ActiveDirectoryService>();
            services.AddSingleton<DataProtectionService>();
            services.AddSingleton<MultiFactorApiClient>();
            services.AddSingleton<AuthService>();
            services.AddApplicationCache();

            services.AddTransient<IMultiFactorApi, MultiFactorApi>()
                .AddTransient<MultifactorHttpClientAdapterFactory>();
            services.AddTransient<IMultifactorIdpApi, MultifactorIdpApi>()
                .AddTransient<MultifactorIdpHttpClientAdapterFactory>();

            services.AddSingleton<ContentCache>();

            services.AddSingleton<PasswordPolicyService>();

            services.AddHttpClient();
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
        
        private static void ConfigureHttpClients(ServiceCollection services)
        {
            WebProxy proxy = null;
            var proxySetting = Configuration.Current.MultiFactorApiProxy;
            if (!string.IsNullOrWhiteSpace(proxySetting))
            {
                proxy = BuildProxy(proxySetting);
            }

            services
                .AddHttpClient(Constants.HttpClients.MultifactorApi, client =>
                {
                    client.BaseAddress = new Uri(Configuration.Current.MultiFactorApiUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateHttpClientHandler(proxy));
            
            services
                .AddHttpClient(Constants.HttpClients.GoogleCaptcha, client =>
                {
                    client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateHttpClientHandler(proxy));
            
            services
                .AddHttpClient(Constants.HttpClients.YandexCaptcha, client =>
                {
                    client.BaseAddress = new Uri("https://captcha-api.yandex.ru/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateHttpClientHandler(proxy));
            
            services
                .AddHttpClient(Constants.HttpClients.MultifactorIdpApi, client =>
                {
                    client.BaseAddress = new Uri(Configuration.Current.MultiFactorIdpApiUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateHttpClientHandler(proxy));
        }

        private static void ConfigureYandexCaptchaApi(ServiceCollection services)
        {
            services
                .AddTransient<YandexCaptchaApi>()
                .AddTransient<YandexHttpClientAdapterFactory>();
        }

        private static void ConfigureGoogleApi(ServiceCollection services)
        {
            services
                .AddTransient<GoogleReCaptcha2Api>()
                .AddTransient<GoogleCaptchaHttpClientAdapterFactory>();
        }
        
        private static HttpClientHandler CreateHttpClientHandler(WebProxy webProxy = null)
        {
            var handler = new HttpClientHandler();
            handler.Proxy = webProxy;
            return handler;
        }
        private static void ConfigureApplicationSerivces(ServiceCollection services)
        {
            services
                .AddSingleton<SafeHttpContextAccessor>()
                .AddSingleton<HttpClientTokenProvider>()
                .AddSingleton<TokenVerifier>()
                .AddSingleton<TokenClaimsAccessor>()
                .AddSingleton<DataProtection>()
                .AddSingleton<ICredentialVerifier, CredentialVerifierAdapter>()
                .AddSingleton<UserPasswordChanger>()
                .AddSingleton<ForgottenPasswordChanger>();

            services.AddSingleton<IApplicationCache, Core.Caching.ApplicationCache>();

            services
                .AddSingleton<ClaimsProvider>()
                .AddSingleton<IClaimsSource, MultiFactorClaimsSource>()
                .AddSingleton<IClaimsSource, SsoClaimsSource>()
                .AddSingleton<IClaimsSource, AdditionalClaimsSource>();

            services.AddSingleton<AdditionalClaimDescriptorsProvider>();
            services.AddSingleton<AdditionalClaimsMetadata>();

            services.AddSingleton<IApplicationValuesContext, ClaimValuesContext>();
            services.AddSingleton<ApplicationGlobalValuesProvider>();
            services.AddSingleton<ClaimConditionEvaluator>();
        }
        private static void ConfigureStories(ServiceCollection services)
        {
            services
                .AddTransient<SignInStory>()
                .AddTransient<IdentityStory>()
                .AddTransient<RedirectToCredValidationAfter2FaStory>()
                .AddTransient<AuthnStory>()
                .AddTransient<SignOutStory>()
                .AddTransient<LoadProfileStory>()
                .AddTransient<LoadIdpProfileStory>()
                .AddTransient<FilterShowcaseLinksStory>()
                .AddTransient<RecoverPasswordStory>()
                .AddTransient<AuthenticateSessionStory>()
                .AddTransient<CheckExpiredPasswordSessionStory>()
                .AddTransient<ChangeExpiredPasswordStory>()
                .AddTransient<ChangeValidPasswordStory>()
                .AddTransient<SearchExchangeActiveSyncDevicesStory>();
        }

        private static void ConfigureCloudConfiguration(ServiceCollection services)
        {
            services.AddSingleton<IShowcaseSettingsOptions, ShowcaseSettingsOptions>();
            services.AddSingleton<ShowcaseSettingsUpdater>();
        }

        private static WebProxy BuildProxy(string proxyUri)
        {
            var uri = new Uri(proxyUri);
            var proxy = new WebProxy(uri);
            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var credentials = uri.UserInfo.Split(new[] { ':' }, 2);
                proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
            }

            return proxy;
        }
    }
}