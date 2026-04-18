using Microsoft.AspNetCore.Identity;

namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents a user's verdict rating for a deal (custom or live).
    /// References GameDeal via foreign key for data integrity.
    /// </summary>
    public class DealRating
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The game deal this rating belongs to (foreign key)
        /// </summary>
        public Guid GameDealId { get; set; }

        /// <summary>
        /// Navigation property to the GameDeal
        /// </summary>
        public virtual GameDeal GameDeal { get; set; } = null!;

        /// <summary>
        /// The verdict chosen by the user
        /// </summary>
        public DealVerdict Verdict { get; set; }

        /// <summary>
        /// The reason ID for the verdict (1-12)
        /// </summary>
        public int ReasonId { get; set; }

        /// <summary>
        /// Optional text review explaining the verdict
        /// </summary>
        public string? ReviewText { get; set; }

        /// <summary>
        /// User who gave the rating
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the User
        /// </summary>
        public virtual IdentityUser User { get; set; } = null!;

        /// <summary>
        /// When the rating was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the rating was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
