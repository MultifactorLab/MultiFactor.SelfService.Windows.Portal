
namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class Link
    {
        public Link(LinkElement linkElement) 
        {
            Url = linkElement.Url;
            Title = linkElement.Title;
            Image = linkElement.Image;
        }

        public string Url { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
    }
}