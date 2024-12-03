using MultiFactor.SelfService.Windows.Portal.Core;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class PersonalData
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }

        public PersonalData(string name, string email, string phone, PrivacyModeDescriptor privacyModeDescriptor)
        {
            (Name, Email, Phone) = (name, email, phone);
        
            switch (privacyModeDescriptor.Mode)
            {
                case PrivacyMode.Full:
                    Name = null;
                    Email = null;
                    Phone = null;
                    break;
            
                case PrivacyMode.Partial:
                    if (!privacyModeDescriptor.HasField("Name"))
                    {
                        Name = null;
                    }

                    if (!privacyModeDescriptor.HasField("Email"))
                    {
                        Email = null;
                    }

                    if (!privacyModeDescriptor.HasField("Phone"))
                    {
                        Phone = null;
                    }
                    break;
            }
        }
    }
}