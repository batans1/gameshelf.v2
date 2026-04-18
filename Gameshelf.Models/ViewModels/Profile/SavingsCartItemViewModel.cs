namespace GameShelf.Models.ViewModels.Profile
{
    public class SavingsCartItemViewModel
    {
        public Guid GameDealId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string PlatformName { get; set; } = string.Empty;
        public string? DealId { get; set; }
        public Guid? CustomDealId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal DealPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal Savings => Math.Max(0, OriginalPrice - DealPrice);
    }
}
