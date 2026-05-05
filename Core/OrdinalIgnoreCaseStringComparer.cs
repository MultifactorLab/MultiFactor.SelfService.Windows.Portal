using System.Collections.Generic;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public class OrdinalIgnoreCaseStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}
