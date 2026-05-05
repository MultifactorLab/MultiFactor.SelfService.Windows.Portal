using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class ShowcaseSettingsDto
    {
        public bool Enabled { get; set; }
        public IEnumerable<ShowcaseLinkDto> ShowcaseLinks { get; set; }
    }

    public class ShowcaseLinkDto
    {
        public string ResourceId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public bool OpenInNewTab { get; set; }
    }
}