using System.Linq;
using System;
using System.Linq.Expressions;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class ClassPropertyAccessor
    {
        public static string GetPropertyPath<TClass, TProperty>(Expression<Func<TClass, TProperty>> propertySelector, string separator = ":") where TClass : class
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }
            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }
            if (propertySelector.Body.NodeType != ExpressionType.MemberAccess) throw new Exception("Invalid property name");

            var path = propertySelector.ToString().Split('.').Skip(1) ?? Array.Empty<string>();
            return string.Join(separator, path);
        }
    }
}
