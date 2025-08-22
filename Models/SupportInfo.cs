using System;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class SupportInfo
    {
        public string AdminName { get; private set; }
        public string AdminEmail { get; private set; }
        public string AdminPhone { get; private set; }

        public SupportInfo(string adminName, string adminEmail, string adminPhone)
        {
            AdminName = adminName;
            AdminEmail = adminEmail;
            AdminPhone = adminPhone;
        }
        
    }
}
