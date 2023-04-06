
using MultiFactor.SelfService.Windows.Portal.Services.API;
using System.Text;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class SingleSignOnDto
    {
        public bool HasSamlSession() => !string.IsNullOrEmpty(SamlSessionId);
        public bool HasOidcSession() => !string.IsNullOrEmpty(OidcSessionId);
        public string SamlSessionId { get; set; }
        public string OidcSessionId { get; set; }
        public override string ToString()
        {
            var sb = new StringBuilder(string.Empty);
            if (HasSamlSession()) sb.Append($"?{MultiFactorClaims.SamlSessionId}={SamlSessionId}");
            if (HasOidcSession()) sb.Append($"?{MultiFactorClaims.OidcSessionId}={OidcSessionId}");
            return sb.ToString();
        }
    }
}