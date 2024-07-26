using System;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredIfNotNullAttribute : ValidationAttribute
    {
        private readonly string _propertyName;

        public RequiredIfNotNullAttribute(string propertyName) : base(propertyName)
        {
            _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null) throw new ArgumentNullException(nameof(validationContext));
            var property = validationContext.ObjectType.GetProperty(_propertyName);

            if (property == null)
            {
                throw new NotSupportedException($"Can't find {_propertyName} on searched type: {validationContext.ObjectType.Name}");
            }

            var requiredIfTypeActualValue = property.GetValue(validationContext.ObjectInstance);

            if (requiredIfTypeActualValue == null)
            {
                return ValidationResult.Success;    
            }

            return value == null
                ? new ValidationResult(FormatErrorMessage(validationContext.DisplayName))
                : ValidationResult.Success;
        }
    }
}