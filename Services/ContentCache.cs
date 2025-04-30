using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Extensions;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public enum ContentCategory
    {
        None,
        PasswordRequirements
    }

    public class ContentCache
    {
        private static readonly string _directory = $"{Constants.WORKING_DIRECTORY}/Content";
        private readonly Lazy<Dictionary<string, string[]>> _loadedContent;

        public ContentCache(ILogger logger)
        {
            _loadedContent = new Lazy<Dictionary<string, string[]>>(() =>
            {
                try
                {
                    if (!Directory.Exists(_directory))
                    {
                        Directory.CreateDirectory(_directory);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to create content directory '{Directory:l}'", _directory);
                }

                var files = Directory.GetFiles(_directory, "*.content", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    return new Dictionary<string, string[]>();
                }

                var dict = new Dictionary<string, string[]>();
                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file, Encoding.UTF8);
                        var key = Path.GetFileNameWithoutExtension(file);
                        dict[key] = lines;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to load content from file '{File:l}'", file);
                        continue;
                    }
                }

                return dict;
            });
        }

        public string[] GetLines(ContentCategory category)
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var configRequirements = GetPasswordRequirementsFromConfig(culture.TwoLetterISOLanguageName);
            if (configRequirements.Any())
            {
                return configRequirements;
            }
            
            var key = GetKey(category, culture.TwoLetterISOLanguageName);
            return _loadedContent.Value.ContainsKey(key) ? _loadedContent.Value[key] : Array.Empty<string>();
        }

        private string[] GetPasswordRequirementsFromConfig(string culture)
        {
            var requirements = new List<string>();
            foreach (var requirement in Configuration.Current.PasswordRequirements.GetAllRequirements())
            {
                if (requirement?.Enabled == true)
                {
                    var message = requirement.GetLocalizedMessage(culture);
                    if (!string.IsNullOrEmpty(message))
                    {
                        requirements.Add(message);
                    }
                }
            }
            return requirements.ToArray();
        }

        private static string GetKey(ContentCategory category, string culture)
        {
            switch (category)
            {
                case ContentCategory.PasswordRequirements:
                    return $"pwd.{culture}";

                case ContentCategory.None:
                default:
                    return string.Empty;
            }
        }
    }
}
