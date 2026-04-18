namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents a click on a "View deal" button for tracking analytics.
    /// </summary>
    public class DealClick
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
        /// The game title
        /// </summary>
        public string GameTitle { get; set; } = string.Empty;

        /// <summary>
        /// The deal URL that was clicked
        /// </summary>
        public string DealUrl { get; set; } = string.Empty;

        /// <summary>
        /// When the click occurred
        /// </summary>
        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: User ID if user was logged in
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Optional: IP address for analytics
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
