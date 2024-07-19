using System;

namespace MultiFactor.SelfService.Windows.Portal.Features.PreAuthnModeFeature
{
    public class PreAuthnModeDescriptor
    {
        private PreAuthnMode Mode { get; }
        public PreAuthnModeSettings Settings { get; }

        private PreAuthnModeDescriptor(PreAuthnMode mode, PreAuthnModeSettings settings)
        {
            Mode = mode;
            Settings = settings;
        }

        public static PreAuthnModeDescriptor Create(string value, PreAuthnModeSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return new PreAuthnModeDescriptor(PreAuthnMode.None, settings);
            }

            var mode = GetMode(value);
            return new PreAuthnModeDescriptor(mode, settings);
        }

        public bool IsPreAuthEnable() => Mode != PreAuthnMode.None;
        
        private static PreAuthnMode GetMode(string value)
        {
            Enum.TryParse<PreAuthnMode>(value, true, out var parsed);
            return parsed;
        }

        public override string ToString() => Mode.ToString();

        public static string DisplayAvailableModes() => string.Join(", ", Enum.GetNames(typeof(PreAuthnMode)));
    }
}