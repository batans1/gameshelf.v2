using System.Text.RegularExpressions;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Business.Services.Moderation;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameShelf.Business.Services.Implementations
{
    public class ReviewModerationService : IReviewModerationService
    {
        private const int WarningsPerStrike = 6;
        private static readonly TimeSpan FirstStrikeTimeout = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan SecondStrikeTimeout = TimeSpan.FromHours(6);
        private static readonly TimeSpan ThirdStrikeTimeout = TimeSpan.FromHours(24);

        // Extend as needed.
        private static readonly string[] ProfanityWords =
        [
            "fuck", "fucks", "fucked", "fucking", "motherfucker", "mf",
            "shit", "shits", "shitty", "bullshit",
            "bitch", "bitches", "bitchy",
            "asshole", "assholes", "ass", "dumbass", "jackass",
            "bastard", "bastards",
            "dick", "dicks", "dickhead",
            "cunt", "cunts",
            "prick", "pricks",
            "piss", "pissed", "pissing",
            "slut", "sluts",
            "whore", "whores",
            "wanker",
            "twat",
            "retard", "retarded",
            "nigger", "nigga",
            "fag", "faggot"
        ];

        private readonly ApplicationDbContext _dbContext;

        public ReviewModerationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ModerationOutcome> ModerateReviewAsync(string userId, string reviewText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                return new ModerationOutcome { ContainsProfanity = false, SanitizedText = reviewText };
            }

            var now = DateTime.UtcNow;
            var status = await _dbContext.UserModerationStatuses.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            var isNewStatus = status == null;
            status ??= new UserModerationStatus { UserId = userId };

            if (status.TimeoutUntilUtc.HasValue && status.TimeoutUntilUtc > now)
            {
                var remaining = status.TimeoutUntilUtc.Value - now;
                throw new ReviewModerationException(
                    $"You are timed out from posting text reviews for {FormatDuration(remaining)} due to repeated profanities.",
                    429);
            }

            var containsProfanity = ContainsProfanity(reviewText);
            var sanitized = CensorProfanity(reviewText);

            if (!containsProfanity)
            {
                return new ModerationOutcome
                {
                    ContainsProfanity = false,
                    SanitizedText = sanitized
                };
            }

            if (status.StrikeCount >= 3)
            {
                status.TimeoutUntilUtc = now.Add(ThirdStrikeTimeout);
                status.UpdatedAt = now;
                UpsertStatus(status, isNewStatus);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new ModerationOutcome
                {
                    ContainsProfanity = true,
                    IsTimedOut = true,
                    SanitizedText = sanitized,
                    UserMessage = "Your review contains profanities. You have 0 warnings left before 24 hours timeout."
                };
            }

            status.WarningsInCurrentStrike++;
            status.UpdatedAt = now;

            var warningsLeft = Math.Max(0, WarningsPerStrike - status.WarningsInCurrentStrike);
            if (warningsLeft == 0)
            {
                status.StrikeCount++;
                status.WarningsInCurrentStrike = 0;
                var timeout = GetTimeoutForStrike(status.StrikeCount);
                status.TimeoutUntilUtc = now.Add(timeout);
                UpsertStatus(status, isNewStatus);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new ModerationOutcome
                {
                    ContainsProfanity = true,
                    IsTimedOut = true,
                    SanitizedText = sanitized,
                    UserMessage = $"Your review contains profanities. You have 0 warnings left before {FormatDuration(timeout)} timeout."
                };
            }

            UpsertStatus(status, isNewStatus);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var nextTimeout = GetTimeoutForStrike(status.StrikeCount + 1);
            return new ModerationOutcome
            {
                ContainsProfanity = true,
                SanitizedText = sanitized,
                UserMessage = $"Your review contains profanities. You have {warningsLeft} warnings left before {FormatDuration(nextTimeout)} timeout."
            };
        }

        private static TimeSpan GetTimeoutForStrike(int strike) =>
            strike switch
            {
                1 => FirstStrikeTimeout,
                2 => SecondStrikeTimeout,
                _ => ThirdStrikeTimeout
            };

        private static bool ContainsProfanity(string text) =>
            ProfanityWords.Any(w => Regex.IsMatch(text, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase));

        private static string CensorProfanity(string text)
        {
            var output = text;
            foreach (var word in ProfanityWords)
            {
                output = Regex.Replace(
                    output,
                    $@"\b{Regex.Escape(word)}\b",
                    m => new string('*', m.Value.Length),
                    RegexOptions.IgnoreCase);
            }
            return output;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                var h = (int)Math.Ceiling(duration.TotalHours);
                return h == 1 ? "1 hour" : $"{h} hours";
            }

            var m = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));
            return m == 1 ? "1 minute" : $"{m} minutes";
        }

        private void UpsertStatus(UserModerationStatus status, bool isNewStatus)
        {
            if (isNewStatus)
            {
                _dbContext.UserModerationStatuses.Add(status);
                return;
            }

            _dbContext.UserModerationStatuses.Update(status);
        }
    }
}
