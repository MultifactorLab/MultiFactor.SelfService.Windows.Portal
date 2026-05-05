using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    /// <summary>
    /// Protect sensitive data.
    /// </summary>
    public class DataProtection
    {
        private readonly DataProtectionService _dataProtectionService;

        public DataProtection(DataProtectionService dataProtectionService)
        {
            _dataProtectionService = dataProtectionService;
        }

        public string Protect(string data, string protectorName)
        {
            return _dataProtectionService.Protect(data);
        }

        public string Unprotect(string data, string protectorName)
        {
            return _dataProtectionService.Unprotect(data);
        }
    }
}