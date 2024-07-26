using System;
using System.Collections.ObjectModel;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class LdapProfile
    {
        private readonly ILogger _logger;
        public LdapProfile(LdapIdentity baseDn, LdapAttributes attributes, ILogger logger)
        {
            BaseDn = baseDn ?? throw new ArgumentNullException(nameof(baseDn));
            LdapAttrs = attributes ?? throw new ArgumentNullException(nameof(attributes));
            _logger = logger;
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
                _logger.Warning(aore,
                    "Something wrong with password expiration date: 'msDS-UserPasswordExpiryTimeComputed'={PasswordExpirationRawValue}",
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
        private LdapAttributes LdapAttrs { get; }
        
        public ReadOnlyCollection<string> MemberOf => LdapAttrs.GetValues("memberOf");
    }
}