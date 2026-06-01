namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums
{
    public enum LoginCompletedAction
    {
        Error,
        Authenticated,
        BypassSaml,
        BypassOidc,
        ChangePassword
    }
}