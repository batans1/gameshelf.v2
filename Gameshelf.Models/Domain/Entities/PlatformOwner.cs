using Microsoft.AspNetCore.Identity;

namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents the link between a platform and a user (owner/admin).
    /// </summary>
    public class PlatformOwner
    {
        /// <summary>
        /// Foreign key to the Platform.
        /// </summary>
        public Guid PlatformId { get; set; }

        /// <summary>
        /// Navigation property to the Platform.
        /// </summary>
        public virtual Platform Platform { get; set; } = null!;

        /// <summary>
        /// Foreign key to the User (IdentityUser uses string Id by default).
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the User.
        /// </summary>
        public virtual IdentityUser User { get; set; } = null!;
    }
}
