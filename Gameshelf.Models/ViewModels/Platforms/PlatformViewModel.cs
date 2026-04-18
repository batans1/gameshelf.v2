using System.ComponentModel.DataAnnotations;

namespace GameShelf.Models.ViewModels.Platforms
{
    /// <summary>
    /// Represents the data returned when retrieving a game platform.
    /// </summary>
    public class PlatformViewModel
    {
        public Guid Id { get; set; }

        [MinLength(2)]
        [MaxLength(100)]
        [Required]
        public string Name { get; set; } = string.Empty;

        [MinLength(5)]
        [MaxLength(500)]
        [Required]
        [Url]
        public string WebsiteUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        [Url]
        public string SupportUrl { get; set; } = string.Empty;

        public List<PlatformImageViewModel> Images { get; set; } = new();
        public List<PlatformOwnerDto> Owners { get; set; } = new();
    }
}
