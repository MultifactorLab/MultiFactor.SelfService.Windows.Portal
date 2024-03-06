using System;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class Token
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public bool MustChangePassword { get; set; }
        public bool MustResetPassword { get; set; }
        public DateTime? PasswordExpirationDate { get; set; }
        public DateTime ValidTo { get; set; }
    }
}