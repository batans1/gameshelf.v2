using Microsoft.AspNetCore.Identity;

namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents a user's rating (1-5 stars) for a game deal.
    /// Ratings are valid until the deal is no longer on sale.
    /// </summary>
    public class GameRating
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The deal ID from external API (e.g., CheapShark DealId)
        /// </summary>
        public string DealId { get; set; } = string.Empty;

        /// <summary>
        /// The platform/store name (e.g., "Steam", "GOG")
        /// </summary>
        public string StoreName { get; set; } = string.Empty;

        /// <summary>
        /// Rating value (1-5 stars)
        /// </summary>
        public int Rating { get; set; }

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
