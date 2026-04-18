namespace GameShelf.Models.ViewModels.Platforms
{
    public class DealClickViewModel
    {
        public string DealId { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string DealUrl { get; set; } = string.Empty;
        public int ClickCount { get; set; }
        public DateTime? LastClickedAt { get; set; }
    }
}
