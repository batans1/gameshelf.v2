namespace GameShelf.Models.Domain.Entities
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
