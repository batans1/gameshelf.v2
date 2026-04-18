using GameShelf.Models.ViewModels.DealRatings;

namespace GameShelf.Models.ViewModels.Profile
{
    public class PublicProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool HasCustomAvatar { get; set; }
        public int TotalRatings { get; set; }
        public int TextReviewsCount { get; set; }
        public List<DealReviewViewModel> Reviews { get; set; } = [];
    }
}
