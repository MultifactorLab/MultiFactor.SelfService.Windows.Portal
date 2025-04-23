namespace MultiFactor.SelfService.Windows.Portal.Configurations.Models
{
    public class PasswordRequirements
    {
        public bool RequiresUpperCaseLetters { get; set; }

        public bool RequiresLowerCaseLetters { get; set; }

        public bool RequiresDigits { get; set; }

        public bool RequiresSpecialSymbol { get; set; }

        public string RequiresPasswordLength { get; set; }
        
        public int MinLength { get; set; }
        
        public int MaxLength { get; set; }
    }
} 