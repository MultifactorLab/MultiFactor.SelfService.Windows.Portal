using Newtonsoft.Json;
using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha.Dto
{
    public class VerifyCaptchaResponseDto
    {
        /// <summary>
        /// true|false.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ).
        /// </summary>
        [JsonProperty("challenge_ts")]
        public string ChallengeTs { get; set; }

        /// <summary>
        /// The hostname of the site where the reCAPTCHA was solved.
        /// </summary>
        [JsonProperty("hostname")]
        public string HostName { get; set; }

        /// <summary>
        /// Optional.
        /// </summary>
        [JsonProperty("error-codes")]
        public IReadOnlyList<string> ErrorCodes { get; set; }
    }
}
