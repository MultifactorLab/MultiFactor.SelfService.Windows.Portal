namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums
{
    public enum LoginAction
    {
        Error,
        Authenticated,
        MfaRequired,
        BypassSaml,
        BypassOidc,
        ChangePassword,
        AccessDenied
    }
}