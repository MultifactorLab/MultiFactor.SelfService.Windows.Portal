using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class LdapProfile
    {
        private const int PasswordExpiredFlag = 0x800000;
        private readonly ILogger _logger;
        private string _identityAttribute;
        public LdapProfile(LdapIdentity baseDn, LdapAttributes attributes, ILogger logger, string identityAttribute)
        {
            BaseDn = baseDn ?? throw new ArgumentNullException(nameof(baseDn));
            LdapAttrs = attributes ?? throw new ArgumentNullException(nameof(attributes));
            _logger = logger;
            _identityAttribute = identityAttribute;
        }

        public DateTime? PasswordExpirationDate()
        {
            if (PasswordExpirationRawValue == null ||
                !long.TryParse(PasswordExpirationRawValue, out var passwordExpirationInt))
            {
                return DateTime.MaxValue;
            }

            try
            {
                return DateTime.FromFileTime(passwordExpirationInt);
            }
            catch (ArgumentOutOfRangeException aore)
            {
                // inconsistency between the parsing function and AD value
                _logger.Warning("Something wrong with password expiration date: 'msDS-UserPasswordExpiryTimeComputed'={PasswordExpirationRawValue}",
                    PasswordExpirationRawValue);
                return DateTime.MaxValue;
            }
        }

        private string PasswordExpirationRawValue => LdapAttrs.GetValue("msDS-UserPasswordExpiryTimeComputed");

        public LdapIdentity BaseDn { get; }
        public string DistinguishedName => LdapAttrs.GetValue("distinguishedname");
        public string Upn => LdapAttrs.GetValue("userprincipalname");
        public string DisplayName => LdapAttrs.GetValue("displayname");
        public string Email => LdapAttrs.GetValue("mail");
        public string Phone => LdapAttrs.GetValue("telephoneNumber");
        public string Mobile => LdapAttrs.GetValue("mobile");
        public bool UserMustChangePassword()
        {
            // = "User must change password at next logon" setting
            var userMustChangePasswordHasValue = int.TryParse(LdapAttrs.GetValue("pwdLastSet"), out var pwdLastSet);
            if (userMustChangePasswordHasValue && pwdLastSet == 0)
                return true;

            if (PasswordExpirationDate() < DateTime.Now)
                return true;
            return false;
        }

        private LdapAttributes LdapAttrs { get; }

        public ReadOnlyCollection<string> MemberOf => LdapAttrs.GetValues("memberOf");
        public string OverridenIdentity => string.IsNullOrWhiteSpace(_identityAttribute) ? null : LdapAttrs.GetValue(_identityAttribute);

    }
}