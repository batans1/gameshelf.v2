using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameShelf.Models.Domain.Entities
{
    /// <summary>
    /// Represents an image/logo associated with a game platform
    /// </summary>
    public class PlatformImage
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The file system path or URL to the image
        /// </summary>
        [Required]
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key for the Platform
        /// </summary>
        public Guid PlatformId { get; set; }

        /// <summary>
        /// Navigation property
        /// </summary>
        [ForeignKey(nameof(PlatformId))]
        public virtual Platform Platform { get; set; } = null!;
    }
}
