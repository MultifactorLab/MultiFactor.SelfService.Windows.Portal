namespace MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging
{
    public class PasswordChangingResult

    {
        public bool Success { get; set; }
        public string ErrorReason { get; set; }

        public PasswordChangingResult(bool success, string errorReason) 
        { 
            Success = success;
            ErrorReason = errorReason;
        }
    }
}
