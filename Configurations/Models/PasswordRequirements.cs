using System.Collections.Generic;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Models
{
    public class PasswordRequirements
    {
        public PasswordRequirementElement UpperCaseLetters { get; set; }
        public PasswordRequirementElement LowerCaseLetters { get; set; }
        public PasswordRequirementElement Digits { get; set; }
        public PasswordRequirementElement SpecialSymbols { get; set; }
        public PasswordRequirementElement MinLength { get; set; }
        public PasswordRequirementElement MaxLength { get; set; }
        
        public IEnumerable<PasswordRequirementElement> NotifyingElements { get; set;}

        public IEnumerable<PasswordRequirementElement> GetAllRequirements()
        {
            return new[]
                {
                    UpperCaseLetters,
                    LowerCaseLetters,
                    Digits,
                    SpecialSymbols,
                    MinLength,
                    MaxLength
                }
                .Concat(NotifyingElements ?? Enumerable.Empty<PasswordRequirementElement>());
        }
    }
} 