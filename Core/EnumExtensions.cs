using System;
using System.ComponentModel;
using System.Reflection;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class EnumExtensions
    {
        public static string GetEnumDescription(this Enum enumValue)
        {
            var str = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(str);
            var attr = fieldInfo.GetCustomAttribute<DescriptionAttribute>(false);
            return attr?.Description ?? str;
        }
    }
}