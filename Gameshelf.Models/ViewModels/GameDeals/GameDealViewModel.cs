namespace GameShelf.Models.ViewModels.GameDeals
{
    public class GameDealViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public int DisplayOrder { get; set; }
        public string? ImageUrl { get; set; }
        public string? DealUrl { get; set; }
        public Guid PlatformId { get; set; }
        public string PlatformName { get; set; } = string.Empty;
    }
}
