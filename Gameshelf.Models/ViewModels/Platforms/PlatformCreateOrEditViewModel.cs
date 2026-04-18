using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GameShelf.Models.ViewModels.Platforms
{
    /// <summary>
    /// Represents the data required when creating or editing a platform.
    /// </summary>
    public class PlatformCreateOrEditViewModel
    {
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

        [Display(Name = "Platform logos")]
        public List<IFormFile>? Images { get; set; }

        public int MainImageIndex { get; set; } = 0;
        public List<PlatformImageViewModel> ExistingImages { get; set; } = new();
        public List<string> SelectedOwnerIds { get; set; } = new();
        public List<SelectListItem> AvailableOwners { get; set; } = new();
    }
}
