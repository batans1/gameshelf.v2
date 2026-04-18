using System.ComponentModel.DataAnnotations;

namespace GameShelf.Models.ViewModels.GameDeals
{
    public class GameDealCreateOrEditViewModel
    {
        [Required]
        public Guid PlatformId { get; set; }

        [Required(ErrorMessage = "The game name is required.")]
        [MaxLength(200, ErrorMessage = "The game name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "The description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater.")]
        [Display(Name = "Price (USD)")]
        public decimal PriceUsd { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Original Price (USD)")]
        public decimal? OriginalPriceUsd { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        [Url]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Url]
        [Display(Name = "Deal URL")]
        public string? DealUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsAvailable { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
