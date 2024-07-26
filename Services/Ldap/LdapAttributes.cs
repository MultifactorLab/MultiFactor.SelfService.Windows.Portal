using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class LdapAttributes
    {
        private readonly Dictionary<string, List<string>> _attrs = new Dictionary<string, List<string>>();
        
        public string GetValue(string attribute)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var attr = attribute.ToLower(CultureInfo.InvariantCulture);
            return !_attrs.TryGetValue(attr, out var expectedAttr)
                ? default
                : expectedAttr.FirstOrDefault();
        }
        
        public ReadOnlyCollection<string> GetValues(string attribute)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var attr = attribute.ToLower(CultureInfo.InvariantCulture);

            return !_attrs.TryGetValue(attr, out var expectedAttributes)
                ? new ReadOnlyCollection<string>(Array.Empty<string>())
                : expectedAttributes.AsReadOnly();
        }

        public void Add(string attribute, IEnumerable<string> value)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var attr = attribute.ToLower(CultureInfo.InvariantCulture);
            if (!_attrs.ContainsKey(attr))
            {
                _attrs[attr] = new List<string>();
            }

            _attrs[attr].AddRange(value);
        }
    }
}