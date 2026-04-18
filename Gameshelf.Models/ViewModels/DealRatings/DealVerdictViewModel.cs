using GameShelf.Models.Domain.Entities;

namespace GameShelf.Models.ViewModels.DealRatings
{
    /// <summary>
    /// ViewModel for displaying deal verdict statistics
    /// </summary>
    public class DealVerdictViewModel
    {
        public DealVerdict Verdict { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public int ReviewCount { get; set; }
    }
}
