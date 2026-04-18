namespace GameShelf.Models.ViewModels.Profile
{
    public class SavingsCartSummaryViewModel
    {
        public List<SavingsCartItemViewModel> Items { get; set; } = [];
        public decimal TotalDealPrice => Items.Sum(i => i.DealPrice);
        public decimal TotalOriginalPrice => Items.Sum(i => i.OriginalPrice);
        public decimal TotalSavings => Items.Sum(i => i.Savings);
    }
}
