using GameShelf.Business.Services.Interfaces;
using GameShelf.Business.Services.Moderation;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.DealRatings;
using GameShelf.Models.ViewModels.LiveDeals;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class DealRatingService : IDealRatingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILiveDealSyncService _liveDealSyncService;
        private readonly IReviewModerationService _reviewModerationService;

        public DealRatingService(
            ApplicationDbContext dbContext,
            ILiveDealSyncService liveDealSyncService,
            IReviewModerationService reviewModerationService)
        {
            _dbContext = dbContext;
            _liveDealSyncService = liveDealSyncService;
            _reviewModerationService = reviewModerationService;
        }

        
        /// Resolves dealId/customDealId to GameDealId. For custom deals, uses the Guid directly.
        /// For live deals, finds or creates the GameDeal in the database.
       
        private async Task<Guid?> ResolveGameDealIdAsync(string? dealId, Guid? customDealId, string storeName)
        {
            if (customDealId.HasValue)
            {
                // Custom deal verify
                var exists = await _dbContext.GameDeals.AnyAsync(d => d.Id == customDealId.Value);
                return exists ? customDealId.Value : null;
            }

            if (!string.IsNullOrEmpty(dealId))
            {
                // Live deal - normalize DealId
                var normalizedDealId = dealId.Trim();
                try
                {
                    string prev;
                    do
                    {
                        prev = normalizedDealId;
                        normalizedDealId = Uri.UnescapeDataString(normalizedDealId);
                    } while (normalizedDealId != prev && normalizedDealId.Contains('%'));
                }
                catch { }
                normalizedDealId = normalizedDealId.Trim();

                // Find existing GameDeal by DealId
                var gameDeal = await _dbContext.GameDeals
                    .FirstOrDefaultAsync(d => d.Source == DealSource.Live && d.DealId != null && d.DealId.Trim() == normalizedDealId);

                if (gameDeal != null)
                    return gameDeal.Id;

                // If not found, try to get or create it via sync
                try
                {
                    return await _liveDealSyncService.GetOrCreateGameDealIdAsync(normalizedDealId, storeName);
                }
                catch
                {
                    // If creation fails return null
                    return null;
                }
            }

            return null;
        }

        public async Task<DealRatingViewModel?> GetUserRatingAsync(string? dealId, Guid? customDealId, string userId)
        {
            var gameDealId = await ResolveGameDealIdAsync(dealId, customDealId, "");
            if (!gameDealId.HasValue)
            {
                var communityVerdict = await GetCommunityVerdictAsync(dealId, customDealId);
                return new DealRatingViewModel
                {
                    CommunityVerdict = communityVerdict
                };
            }

            var userRating = await _dbContext.DealRatings
                .FirstOrDefaultAsync(r => r.GameDealId == gameDealId.Value && r.UserId == userId);

            var communityVerdictResult = await GetCommunityVerdictAsync(dealId, customDealId);

            if (userRating == null)
            {
                return new DealRatingViewModel
                {
                    CommunityVerdict = communityVerdictResult
                };
            }

            return new DealRatingViewModel
            {
                UserVerdict = userRating.Verdict,
                UserReasonId = userRating.ReasonId,
                UserReviewText = userRating.ReviewText,
                CommunityVerdict = communityVerdictResult
            };
        }

        public async Task<CommunityVerdictViewModel> GetCommunityVerdictAsync(string? dealId, Guid? customDealId)
        {
            var gameDealId = await ResolveGameDealIdAsync(dealId, customDealId, "");
            if (!gameDealId.HasValue)
            {
                return new CommunityVerdictViewModel();
            }

            var ratings = await _dbContext.DealRatings
                .Where(r => r.GameDealId == gameDealId.Value)
                .ToListAsync();

            var totalRatings = ratings.Count;

            if (totalRatings == 0)
            {
                return new CommunityVerdictViewModel();
            }

            var buyNowRatings = ratings.Where(r => r.Verdict == DealVerdict.BuyNow).ToList();
            var waitRatings = ratings.Where(r => r.Verdict == DealVerdict.Wait).ToList();
            var notWorthItRatings = ratings.Where(r => r.Verdict == DealVerdict.NotWorthIt).ToList();

            return new CommunityVerdictViewModel
            {
                BuyNow = new DealVerdictViewModel
                {
                    Verdict = DealVerdict.BuyNow,
                    Count = buyNowRatings.Count,
                    Percentage = totalRatings > 0 ? Math.Round((double)buyNowRatings.Count / totalRatings * 100, 1) : 0,
                    ReviewCount = buyNowRatings.Count(r => !string.IsNullOrEmpty(r.ReviewText))
                },
                Wait = new DealVerdictViewModel
                {
                    Verdict = DealVerdict.Wait,
                    Count = waitRatings.Count,
                    Percentage = totalRatings > 0 ? Math.Round((double)waitRatings.Count / totalRatings * 100, 1) : 0,
                    ReviewCount = waitRatings.Count(r => !string.IsNullOrEmpty(r.ReviewText))
                },
                NotWorthIt = new DealVerdictViewModel
                {
                    Verdict = DealVerdict.NotWorthIt,
                    Count = notWorthItRatings.Count,
                    Percentage = totalRatings > 0 ? Math.Round((double)notWorthItRatings.Count / totalRatings * 100, 1) : 0,
                    ReviewCount = notWorthItRatings.Count(r => !string.IsNullOrEmpty(r.ReviewText))
                },
                TotalRatings = totalRatings
            };
        }

        public async Task<ModerationOutcome?> SetRatingAsync(string? dealId, Guid? customDealId, string storeName, string userId, DealVerdict verdict, int reasonId, string? reviewText)
        {
            // Validate reason matches verdict
            if (!DealRatingReason.IsValidReasonForVerdict(reasonId, verdict))
            {
                throw new ArgumentException($"Reason ID {reasonId} is not valid for verdict {verdict}", nameof(reasonId));
            }

            ModerationOutcome? moderationOutcome = null;
            if (!string.IsNullOrWhiteSpace(reviewText))
            {
                moderationOutcome = await _reviewModerationService.ModerateReviewAsync(userId, reviewText);
                reviewText = moderationOutcome.SanitizedText;
            }

            var gameDealId = await ResolveGameDealIdAsync(dealId, customDealId, storeName);
            if (!gameDealId.HasValue)
            {
                throw new InvalidOperationException($"GameDeal not found for dealId={dealId}, customDealId={customDealId}. Live deals must be synced to database first.");
            }

            var existing = await _dbContext.DealRatings
                .FirstOrDefaultAsync(r => r.GameDealId == gameDealId.Value && r.UserId == userId);

            if (existing != null)
            {
                existing.Verdict = verdict;
                existing.ReasonId = reasonId;
                existing.ReviewText = reviewText;
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.DealRatings.Update(existing);
            }
            else
            {
                var newRating = new DealRating
                {
                    Id = Guid.NewGuid(),
                    GameDealId = gameDealId.Value,
                    UserId = userId,
                    Verdict = verdict,
                    ReasonId = reasonId,
                    ReviewText = reviewText,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.DealRatings.AddAsync(newRating);
            }

            await _dbContext.SaveChangesAsync();
            return moderationOutcome;
        }

        public async Task<IEnumerable<DealReviewViewModel>> GetDealReviewsAsync(string? dealId, Guid? customDealId, DealVerdict? verdictFilter = null)
        {
            var gameDealId = await ResolveGameDealIdAsync(dealId, customDealId, "");
            if (!gameDealId.HasValue)
            {
                return Enumerable.Empty<DealReviewViewModel>();
            }

            IQueryable<DealRating> query = _dbContext.DealRatings
                .Include(r => r.User)
                .Where(r => r.GameDealId == gameDealId.Value && !string.IsNullOrEmpty(r.ReviewText));

            if (verdictFilter.HasValue)
            {
                query = query.Where(r => r.Verdict == verdictFilter.Value);
            }

            var ratings = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ratings.Select(r => new DealReviewViewModel
            {
                Id = r.Id,
                Verdict = r.Verdict,
                ReasonId = r.ReasonId,
                ReasonText = DealRatingReason.GetReasonText(r.ReasonId),
                ReviewText = r.ReviewText,
                UserName = r.User.UserName ?? "Unknown",
                CreatedAt = r.CreatedAt
            });
        }

        public async Task<int> GetRatingCountAsync(string? dealId, Guid? customDealId)
        {
            var gameDealId = await ResolveGameDealIdAsync(dealId, customDealId, "");
            if (!gameDealId.HasValue)
            {
                return 0;
            }

            return await _dbContext.DealRatings
                .Where(r => r.GameDealId == gameDealId.Value)
                .CountAsync();
        }

        public async Task<IEnumerable<DealReviewViewModel>> GetUserReviewsAsync(string userId, bool includeWithoutText)
        {
            var query = _dbContext.DealRatings
                .Include(r => r.User)
                .Where(r => r.UserId == userId);

            if (!includeWithoutText)
            {
                query = query.Where(r => !string.IsNullOrWhiteSpace(r.ReviewText));
            }

            var ratings = await query
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            return ratings.Select(r => new DealReviewViewModel
            {
                Id = r.Id,
                Verdict = r.Verdict,
                ReasonId = r.ReasonId,
                ReasonText = DealRatingReason.GetReasonText(r.ReasonId),
                ReviewText = r.ReviewText,
                UserName = r.User.UserName ?? "Unknown",
                CreatedAt = r.CreatedAt
            });
        }

        public async Task DeleteReviewTextAsync(Guid ratingId)
        {
            var rating = await _dbContext.DealRatings.FirstOrDefaultAsync(r => r.Id == ratingId);
            if (rating == null) return;

            rating.ReviewText = null;
            rating.UpdatedAt = DateTime.UtcNow;
            _dbContext.DealRatings.Update(rating);
            await _dbContext.SaveChangesAsync();
        }
    }
}
