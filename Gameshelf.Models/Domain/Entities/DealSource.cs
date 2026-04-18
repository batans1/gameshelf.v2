namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents the source of a game deal.
    /// </summary>
    public enum DealSource
    {
        /// <summary>
        /// Custom deal manually added by platform owner/admin
        /// </summary>
        Custom = 1,
        
        /// <summary>
        /// Live deal from external API (e.g., CheapShark)
        /// </summary>
        Live = 2
    }
}
