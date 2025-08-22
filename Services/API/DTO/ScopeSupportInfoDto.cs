using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    public class ScopeSupportInfoDto
    {
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminPhone { get; set; }

        public static SupportInfo ToModel(ScopeSupportInfoDto dto)
        {
            return new SupportInfo(dto.AdminName, dto.AdminEmail, dto.AdminPhone);
        }
    }    
}