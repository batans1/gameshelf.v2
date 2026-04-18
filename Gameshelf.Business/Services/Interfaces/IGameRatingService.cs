namespace GameShelf.Business.Services.Interfaces
{
    public interface IGameRatingService
    {
        Task<int?> GetUserRatingAsync(string dealId, string userId);
        Task<double> GetAverageRatingAsync(string dealId);
        Task<int> GetRatingCountAsync(string dealId);
        Task SetRatingAsync(string dealId, string storeName, string userId, int rating);
    }
}
