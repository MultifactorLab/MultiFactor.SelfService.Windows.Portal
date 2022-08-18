using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public static class GoogleSiteverifyErrorCode
    {
        private static readonly IReadOnlyDictionary<string, string> _metadata;

        static GoogleSiteverifyErrorCode()
        {
            _metadata = GetMetadata();
        }

        [Description("The secret parameter is missing.")]
        public const string MissingInputSecret = "missing-input-secret";

        [Description("The secret parameter is invalid or malformed.")]
        public const string InvalidInputSecret = "invalid-input-secret";

        [Description("The response parameter is missing.")]
        public const string MissingInputResponse = "missing-input-response";

        [Description("The response parameter is invalid or malformed.")]
        public const string InvalidInputResponse = "invalid-input-response";

        [Description("The request is invalid or malformed.")]
        public const string BadRequest = "bad-request";

        [Description("The response is no longer valid: either is too old or has been used previously.")]
        public const string TimeoutOrDuplicate = "timeout-or-duplicate";

        public static string GetDescription(string errorCode)
        {
            return _metadata.ContainsKey(errorCode) ? _metadata[errorCode] : errorCode;
        }

        private static IReadOnlyDictionary<string, string> GetMetadata()
        {
            return typeof(GoogleSiteverifyErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.IsLiteral && !x.IsInitOnly)
                .Select(x => new
                {
                    code = x.GetValue(null) as string ?? string.Empty,
                    description = x.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                })
                .Where(x => x.code != string.Empty && x.description != string.Empty)
                .ToDictionary(k => k.code, v => v.description);
        }
    }
}
