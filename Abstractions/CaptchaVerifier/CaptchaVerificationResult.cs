namespace MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier
{
    public class CaptchaVerificationResult
    {
        public bool Success { get; }
        public string Message { get; }

        private CaptchaVerificationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static CaptchaVerificationResult CreateSuccess()
        {
            return new CaptchaVerificationResult(true, null);
        }

        public static CaptchaVerificationResult CreateFail(string message)
        {
            return new CaptchaVerificationResult(false, message);
        }
    }
}
