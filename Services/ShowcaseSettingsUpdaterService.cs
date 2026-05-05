using Serilog;
using System.Threading.Tasks;
using System.Threading;
using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Windows.Portal.Options;
using MultiFactor.SelfService.Windows.Portal.Settings;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Web.Hosting;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class ShowcaseSettingsUpdater
    {
        private readonly IMultiFactorApi _multiFactorApi;
        private readonly IShowcaseSettingsOptions _options;
        private readonly ILogger _logger;

        private readonly TimeSpan _period = TimeSpan.FromSeconds(90);
        private CancellationTokenSource _cts;

        public ShowcaseSettingsUpdater(
            IMultiFactorApi api,
            IShowcaseSettingsOptions options,
            ILogger logger)
        {
            _multiFactorApi = api;
            _options = options;
            _logger = logger;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Loop(_cts.Token));
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task Loop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var settings = await _multiFactorApi.GetShowcaseSettingsAsync();
                    _options.Update(settings);
                    await UpdateLogos(settings);

                    _logger.Information("Showcase settings updated");
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to update settings", ex);
                }

                await Task.Delay(_period, ct);
            }
        }

        private async Task UpdateLogos(ShowcaseSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            var localFolder = HostingEnvironment.MapPath("~/content/images/showcase");

            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            var cloudFileNames = settings.Links.Select(x => x.Image).ToArray();
            var localFileNames = Directory.GetFiles(localFolder)
                .Select(file => Path.GetFileName(file))
                .ToArray();

            var missingFiles = cloudFileNames.Except(localFileNames).ToArray();
            foreach (var fileName in missingFiles)
            {
                try
                {
                    var data = await _multiFactorApi.GetShowcaseLogoAsync(fileName);
                    if (data is null)
                    {
                        continue;
                    }

                    File.WriteAllBytes(Path.Combine(localFolder, fileName), data);
                }
                catch (HttpRequestException ex)
                {
                    _logger.Warning(ex, "Failed to load showcase logo '{fileName}'", fileName);
                }
            }

            var extraFiles = localFileNames.Except(cloudFileNames).ToArray();
            foreach (var filename in extraFiles)
            {
                File.Delete(Path.Combine(localFolder, filename));
            }
        }
    }
}