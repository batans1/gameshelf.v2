using GameShelf.Models.Domain.Entities;

namespace GameShelf.Models.ViewModels.DealRatings
{
    /// <summary>
    /// ViewModel for the community verdict panel showing percentages and counts
    /// </summary>
    public class CommunityVerdictViewModel
    {
        public DealVerdictViewModel BuyNow { get; set; } = new() { Verdict = DealVerdict.BuyNow };
        public DealVerdictViewModel Wait { get; set; } = new() { Verdict = DealVerdict.Wait };
        public DealVerdictViewModel NotWorthIt { get; set; } = new() { Verdict = DealVerdict.NotWorthIt };
        public int TotalRatings { get; set; }
    }
}
