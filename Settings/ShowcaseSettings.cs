namespace MultiFactor.SelfService.Windows.Portal.Settings
{
    public class ShowcaseSettings
    {
        public bool Enabled { get; set; }
        public ShowcaseLink[] Links { get; set; } = new ShowcaseLink[0];
    }

    public class ShowcaseLink
    {
        public string ResourceId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public bool OpenInNewTab { get; set; }
    }
}