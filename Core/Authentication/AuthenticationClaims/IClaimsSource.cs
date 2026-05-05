using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims
{
    public interface IClaimsSource
    {
        IReadOnlyDictionary<string, string> GetClaims();
    }
}
