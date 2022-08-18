using System.Threading.Tasks;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier
{
    public interface ICaptchaVerifier
    {
        Task<CaptchaVerificationResult> VerifyCaptchaAsync(HttpRequestBase request);
    }
}
