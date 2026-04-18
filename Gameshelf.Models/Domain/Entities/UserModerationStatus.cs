namespace GameShelf.Models.Domain.Entities
{
    public class UserModerationStatus
    {
        public string UserId { get; set; } = string.Empty;
        public int StrikeCount { get; set; }
        public int WarningsInCurrentStrike { get; set; }
        public DateTime? TimeoutUntilUtc { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
