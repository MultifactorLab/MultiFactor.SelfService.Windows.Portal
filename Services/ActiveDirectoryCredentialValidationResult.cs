using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using System;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Active Directory credential validation
    /// </summary>
    public class ActiveDirectoryCredentialValidationResult
    {
        public bool IsAuthenticated { get; private set; }
        public bool IsBypass { get; private set; }
        public bool UserMustChangePassword { get; set; }
        public string Reason { get; private set; }

        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Upn { get; set; }
        public string OverriddenIdentity { get; set; }
        public string Phone { get; set; }
        public DateTime? PasswordExpirationDate { get; set; }

        public static ActiveDirectoryCredentialValidationResult Ok()
        {
            return new ActiveDirectoryCredentialValidationResult
            {
                IsAuthenticated = true,
            };
        }

        public static ActiveDirectoryCredentialValidationResult ByPass()
        {
            return new ActiveDirectoryCredentialValidationResult
            {
                IsAuthenticated = true,
                IsBypass = true
            };
        }

        public static ActiveDirectoryCredentialValidationResult KnownError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return UnknownError();
            }

            var pattern = @"data ([0-9a-e]{3})";
            var match = Regex.Match(errorMessage, pattern);

            if (!match.Success || match.Groups.Count != 2)
            {
                return UnknownError(errorMessage);
            }

            var data = match.Groups[1].Value;

            switch (data)
            {
                case "525":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "User not found" };
                case "52e":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Invalid credentials" };
                case "530":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Not permitted to logon at this time" };
                case "531":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Not permitted to logon at this workstation​" };
                case "532":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Password expired", UserMustChangePassword = true };
                case "533":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Account disabled" };
                case "701":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "Account expired" };
                case "773":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "User must change password", UserMustChangePassword = true };
                case "775":
                    return new ActiveDirectoryCredentialValidationResult { Reason = "User account locked" };
            }

            return UnknownError(errorMessage);
        }

        public static ActiveDirectoryCredentialValidationResult UnknownError(string errorMessage = null)
        {
            return new ActiveDirectoryCredentialValidationResult { Reason = errorMessage ?? "Unknown error" };
        }
    }

    public static class ActiveDirectoryValidationResultHelper
    {
        public static ActiveDirectoryCredentialValidationResult Fill(this ActiveDirectoryCredentialValidationResult result, LdapProfile profile, Configuration configuration)
        {
            result.DisplayName = profile.DisplayName;
            result.Email = profile.Email;
            result.Upn = profile.Upn;
            result.PasswordExpirationDate = profile.PasswordExpirationDate();
            result.UserMustChangePassword = profile.UserMustChangePassword();
            if (!string.IsNullOrWhiteSpace(configuration.UseAttributeAsIdentity))
            {
                result.OverriddenIdentity = profile.OverridenIdentity;
            }
            if (configuration.UseActiveDirectoryUserPhone)
            {
                result.Phone = profile.Phone;
            }
            if (configuration.UseActiveDirectoryMobileUserPhone)
            {
                result.Phone = profile.Mobile;
            }
            return result;
        }

        public static string GetIdentity(this ActiveDirectoryCredentialValidationResult adValidationResult, string userName)
        {
            if (!string.IsNullOrWhiteSpace(Configuration.Current.UseAttributeAsIdentity) && string.IsNullOrWhiteSpace(adValidationResult.OverriddenIdentity))
            {
                throw new InvalidOperationException($"Failed to get overridden identity attribute '{Configuration.Current.UseAttributeAsIdentity}' for {userName}.");
            }

            if (!string.IsNullOrWhiteSpace(adValidationResult.OverriddenIdentity))
            {
                return adValidationResult.OverriddenIdentity;
            }

            var identity = userName;
            if (!Configuration.Current.UseUpnAsIdentity) return identity;

            if (string.IsNullOrEmpty(adValidationResult.Upn))
            {
                throw new InvalidOperationException($"Null UPN for user {userName}");
            }

            identity = adValidationResult.Upn;

            return identity;
        }
    }
}