using System.Collections.Generic;
using System;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Core.LdapAttributesCaching
{
    public class LdapAttribute
    {
        public string Name { get; }
        public IReadOnlyList<string> Values { get; }

        private LdapAttribute(string name, IReadOnlyList<string> values)
        {
            Name = name;
            Values = values;
        }

        public static LdapAttribute Create(string name, IEnumerable<string> value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new LdapAttribute(name, value.ToList().AsReadOnly());
        }
    }
}
