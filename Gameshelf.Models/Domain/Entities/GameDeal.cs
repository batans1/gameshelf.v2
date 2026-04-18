namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents a game deal for a platform
    /// </summary>
    public class GameDeal
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAvailable { get; set; } = true;
        public int DisplayOrder { get; set; }
        public string? ImageUrl { get; set; }
        public string? DealUrl { get; set; }

        /// <summary>
        /// The source of this deal (Custom or Live)
        /// </summary>
        public DealSource Source { get; set; } = DealSource.Custom;

        /// <summary>
        /// The external deal ID from API (e.g., CheapShark DealId) for live deals.
        /// Null for custom deals.
        /// </summary>
        public string? DealId { get; set; }

        /// <summary>
        /// The platform name stored for quick access
        /// </summary>
        public string StoreName { get; set; } = string.Empty;

        /// <summary>
        /// When this deal was last synced from external API (for live deals)
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        public Guid PlatformId { get; set; }
        public virtual Platform Platform { get; set; } = null!;

        /// <summary>
        /// Navigation property to ratings for this deal
        /// </summary>
        public virtual ICollection<DealRating> Ratings { get; set; } = new List<DealRating>();
    }
}
