using System.Text.RegularExpressions;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification
{
    public class CredentialVerificationResult
    {
        public bool IsAuthenticated { get; }
        public string Reason { get; private set; }

        public bool IsBypass { get; private set; }
        public bool UserMustChangePassword { get; private set; }
        public DateTime PasswordExpirationDate { get; private set; }
        public string DisplayName { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }
        public string Username { get; private set; }
        public string UserPrincipalName { get; private set; }
        public string CustomIdentity { get; private set; }
        private CredentialVerificationResult(bool isAuthenticated)
        {
            IsAuthenticated = isAuthenticated;
        }

        public static CredentialVerificationResultBuilder CreateBuilder(bool isAuthenticated)
        {
            return new CredentialVerificationResultBuilder(new CredentialVerificationResult(isAuthenticated));
        }

        /// <summary>
        /// Result for bypass second factor (only ssp+sso)
        /// </summary>
        /// <param name="username">Raw username that was entered into the form when logging in</param>
        /// <param name="upn">UPN from LDAP profile</param>
        /// <returns></returns>
        public static CredentialVerificationResult ByPass(string username, string upn, bool mustChangePassword)
        {
            return new CredentialVerificationResult(true)
            {
                IsBypass = true,
                Username = username,
                UserPrincipalName = upn,
                UserMustChangePassword = mustChangePassword
            };
        }

        public static CredentialVerificationResult BeforeAuthn(string username)
        {
            return new CredentialVerificationResult(false)
            {
                Username = username,
            };
        }

        public static CredentialVerificationResult FromKnownError(string errorMessage, string username = null)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return FromUnknownError();
            }

            var pattern = @"data ([0-9a-e]{3})";
            var match = Regex.Match(errorMessage, pattern);

            if (match.Success && match.Groups.Count == 2)
            {
                var data = match.Groups[1].Value;

                var resultBuilder = CreateBuilder(false);
                if (username != null)
                {
                    resultBuilder.SetUsername(username);
                }

                switch (data)
                {
                    case "525":
                        return new CredentialVerificationResult(false) { Reason = "User not found" };
                    case "52e":
                        return new CredentialVerificationResult(false) { Reason = "Invalid credentials" };
                    case "530":
                        return new CredentialVerificationResult(false) { Reason = "Not permitted to logon at this time" };
                    case "531":
                        return new CredentialVerificationResult(false) { Reason = "Not permitted to logon at this workstation​" };
                    case "532":
                        return resultBuilder
                            .SetReason("Password Expired")
                            .SetUserMustChangePassword(true)
                            .Build();
                    case "533":
                        return new CredentialVerificationResult(false) { Reason = "Account disabled" };
                    case "701":
                        return new CredentialVerificationResult(false) { Reason = "Account expired" };
                    case "773":
                        return resultBuilder
                            .SetReason("User must change password")
                            .SetUserMustChangePassword(true)
                            .Build();
                    case "775":
                        return new CredentialVerificationResult(false) { Reason = "User account locked" };
                }
            }

            return FromUnknownError(errorMessage);
        }

        public static CredentialVerificationResult FromUnknownError(string errorMessage = null)
        {
            return new CredentialVerificationResult(false) { Reason = errorMessage ?? "Unknown error" };
        }



        public class CredentialVerificationResultBuilder
        {
            private readonly CredentialVerificationResult _result;

            public CredentialVerificationResultBuilder(CredentialVerificationResult result)
            {
                _result = result ?? throw new ArgumentNullException(nameof(result));
            }

            public CredentialVerificationResultBuilder SetDisplayName(string displayName)
            {
                _result.DisplayName = displayName;
                return this;
            }

            public CredentialVerificationResultBuilder SetEmail(string email)
            {
                _result.Email = email;
                return this;
            }

            public CredentialVerificationResultBuilder SetPhone(string phone)
            {
                _result.Phone = phone;
                return this;
            }

            public CredentialVerificationResultBuilder SetUsername(string username)
            {
                _result.Username = username;
                return this;
            }

            public CredentialVerificationResultBuilder SetCustomIdentity(string identity)
            {
                _result.CustomIdentity = identity;
                return this;
            }

            public CredentialVerificationResultBuilder SetUserMustChangePassword(bool userMustChangePassword)
            {
                _result.UserMustChangePassword = userMustChangePassword;
                return this;
            }

            public CredentialVerificationResultBuilder SetPasswordExpirationDate(DateTime dt)
            {
                _result.PasswordExpirationDate = dt;
                return this;
            }

            public CredentialVerificationResultBuilder SetReason(string reason)
            {
                _result.Reason = reason;
                return this;
            }

            public CredentialVerificationResultBuilder SetUserPrincipalName(string upn)
            {
                _result.UserPrincipalName = upn;
                return this;
            }


            public CredentialVerificationResult Build()
            {
                return _result;
            }
        }
    }
}