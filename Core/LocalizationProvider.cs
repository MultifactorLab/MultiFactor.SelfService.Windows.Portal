using System.Globalization;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class LocalizationProvider
    {
        public static string GetLanguage()
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }
    }
}