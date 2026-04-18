namespace GameShelf.Models.Domain.Entities
{
    public class SavingsCartItem
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid GameDealId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual GameDeal GameDeal { get; set; } = null!;
    }
}
