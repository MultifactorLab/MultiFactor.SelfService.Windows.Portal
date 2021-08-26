using System;
using System.Linq;
using System.Text;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class LdapProfile
    {
        public string DistinguishedName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }

        public LdapIdentity BaseDn { get; set; }

        public string ToCanonicalName()
        {
            var sb = new StringBuilder();
            var parts = DistinguishedName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var domain = parts
                .Where(nc => nc.ToLower().StartsWith("dc="))
                .Select(nc => nc.Substring(3))
                .Aggregate((current, next) => current + "." + next);

            var path = parts
                .Where(nc => !nc.ToLower().StartsWith("dc="))
                .Reverse()
                .Select(nc => nc.Substring(3))
                .Aggregate((current, next) => current + "/" + next);

            return domain + "/" + path;
        }
    }
}