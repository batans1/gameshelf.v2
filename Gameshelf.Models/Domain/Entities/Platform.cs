namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents a game platform provider (e.g. Steam, EA, Epic, GOG).
    /// </summary>
    public class Platform
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the platform's name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the platform's website URL
        /// </summary>
        public string WebsiteUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the platform's support or store URL
        /// </summary>
        public string SupportUrl { get; set; } = string.Empty;

        /// <summary>
        /// Collection of images/logos for the platform
        /// </summary>
        public virtual ICollection<PlatformImage> Images { get; set; } =
            new List<PlatformImage>();

        /// <summary>
        /// Collection of owners (users) assigned to manage this platform.
        /// </summary>
        public virtual ICollection<PlatformOwner> Owners { get; set; } = new List<PlatformOwner>();

        /// <summary>
        /// Collection of game deals (manually added) for this platform.
        /// </summary>
        public virtual ICollection<GameDeal> Deals { get; set; } = new List<GameDeal>();
    }
}
