using GameShelf.Business.Services.Moderation;

namespace GameShelf.Business.Services.Interfaces
{
    public interface IReviewModerationService
    {
        Task<ModerationOutcome> ModerateReviewAsync(string userId, string reviewText, CancellationToken cancellationToken = default);
    }
}
