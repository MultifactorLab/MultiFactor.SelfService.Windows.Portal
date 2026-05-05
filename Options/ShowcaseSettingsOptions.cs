using MultiFactor.SelfService.Windows.Portal.Settings;

namespace MultiFactor.SelfService.Windows.Portal.Options
{
    public interface IShowcaseSettingsOptions
    {
        ShowcaseSettings CurrentValue { get; }
        void Update(ShowcaseSettings settings);
    }

    public class ShowcaseSettingsOptions : IShowcaseSettingsOptions
    {
        private ShowcaseSettings _current;
        private readonly object _lock = new object();

        public ShowcaseSettings CurrentValue
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public void Update(ShowcaseSettings settings)
        {
            lock (_lock)
            {
                _current = settings;
            }
        }
    }
}
