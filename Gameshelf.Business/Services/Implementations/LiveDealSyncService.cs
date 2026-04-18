using System.Text.Json;
using System.Text.Json.Serialization;
using GameShelf.Business.Services.Interfaces;
using GameShelf.Data.Persistance;
using GameShelf.Models.Domain.Entities;
using GameShelf.Models.ViewModels.LiveDeals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameShelf.Business.Services.Implementations
{
    public class LiveDealSyncService : ILiveDealSyncService
    {
        private const string CheapSharkBase = "https://www.cheapshark.com/api/1.0";
        private const int PageSize = 60;
        private const int MaxPages = 15;
        private const int FirstPageNumber = 0;
        private const int DelayBetweenPagesMs = 400;

        private static readonly Dictionary<string, int> PlatformToStoreId = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Steam"] = 1,
            ["GOG"] = 7,
            ["Epic Games"] = 25,
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LiveDealSyncService> _logger;

        public LiveDealSyncService(
            ApplicationDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            ILogger<LiveDealSyncService> logger)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task SyncLiveDealsForPlatformAsync(string platformName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting sync for platform: {Platform}", platformName);

                var platform = await _dbContext.Platforms
                    .FirstOrDefaultAsync(p => p.Name == platformName, cancellationToken);

                if (platform == null)
                {
                    _logger.LogWarning("Platform {Platform} not found in database", platformName);
                    return;
                }

                if (!PlatformToStoreId.TryGetValue(platformName, out var storeId))
                {
                    _logger.LogWarning("Platform {Platform} not supported for API sync", platformName);
                    return;
                }

                var client = _httpClientFactory.CreateClient("CheapShark");
                var dealsList = new List<LiveDealDto>();
                var hadApiErrorOrRateLimit = false;

                for (int page = FirstPageNumber; page < FirstPageNumber + MaxPages; page++)
                {
                    try
                    {
                        var url = $"{CheapSharkBase}/deals?storeID={storeId}&pageSize={PageSize}&pageNumber={page}&onSale=1";
                        var response = await client.GetAsync(url, cancellationToken);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                            var shortenedError = errorBody.Length > 180 ? $"{errorBody[..180]}..." : errorBody;
                            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            {
                                _logger.LogWarning("Rate limited by API. Keeping existing live deals for {Platform}", platformName);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "API returned non-success status code {Status} for {Platform}. Body: {Body}. Keeping existing live deals.",
                                    response.StatusCode, platformName, shortenedError);
                            }

                            hadApiErrorOrRateLimit = true;
                            break;
                        }

                        var json = await response.Content.ReadAsStringAsync(cancellationToken);
                        if (json.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Rate limited (response body). Keeping existing live deals for {Platform}", platformName);
                            hadApiErrorOrRateLimit = true;
                            break;
                        }

                        var apiDeals = JsonSerializer.Deserialize<List<CheapSharkDealResponse>>(json, JsonOptions);
                        if (apiDeals == null || apiDeals.Count == 0)
                        {
                            // No more (or no) deals returned. Treat this as \"no update\" and keep existing ones.
                            break;
                        }

                        foreach (var d in apiDeals.Where(x => x.Savings > 0))
                        {
                            var normalizedDealId = NormalizeDealId(d.DealId ?? "");
                            dealsList.Add(new LiveDealDto
                            {
                                Title = d.Title ?? "",
                                SalePriceUsd = d.SalePrice,
                                NormalPriceUsd = d.NormalPrice,
                                SavingsPercent = d.Savings,
                                ThumbUrl = d.Thumb,
                                DealUrl = string.IsNullOrEmpty(d.DealId) ? "" : $"https://www.cheapshark.com/redirect?dealID={d.DealId}",
                                StoreName = platformName,
                                DealId = normalizedDealId
                            });
                        }

                        if (apiDeals.Count < PageSize) break;
                        if (page < MaxPages)
                            await Task.Delay(DelayBetweenPagesMs, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching {Platform} page {Page}", platformName, page);
                        hadApiErrorOrRateLimit = true;
                        break;
                    }
                }

                if (hadApiErrorOrRateLimit || dealsList.Count == 0)
                {
                    _logger.LogWarning("Live deal sync for {Platform} did not fetch any deals (error/rate limit or empty). Keeping existing live deals unchanged.", platformName);
                    return;
                }

                _logger.LogInformation("Fetched {Count} deals from API for {Platform}", dealsList.Count, platformName);

                var existingDeals = await _dbContext.GameDeals
                    .Where(d => d.PlatformId == platform.Id && d.Source == DealSource.Live && d.DealId != null)
                    .ToListAsync(cancellationToken);

                var apiDealIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var deal in dealsList)
                {
                    if (string.IsNullOrEmpty(deal.DealId)) continue;
                    apiDealIds.Add(deal.DealId.Trim());
                }

                var expiredDeals = existingDeals
                    .Where(d => d.DealId != null && !apiDealIds.Contains(d.DealId.Trim()))
                    .ToList();

                if (expiredDeals.Any())
                {
                    _dbContext.GameDeals.RemoveRange(expiredDeals);
                    _logger.LogInformation("Removing {Count} expired deals for {Platform}", expiredDeals.Count, platformName);
                }

                foreach (var deal in dealsList)
                {
                    if (string.IsNullOrEmpty(deal.DealId)) continue;

                    var normalizedDealId = deal.DealId.Trim();
                    var existingDeal = existingDeals.FirstOrDefault(d => d.DealId != null && d.DealId.Trim() == normalizedDealId);

                    if (existingDeal != null)
                    {
                        existingDeal.Name = deal.Title;
                        existingDeal.Price = deal.SalePriceUsd;
                        existingDeal.OriginalPrice = deal.NormalPriceUsd;
                        existingDeal.DiscountPercent = deal.SavingsPercent > 0 ? (int)Math.Round(deal.SavingsPercent) : null;
                        existingDeal.ImageUrl = deal.ThumbUrl;
                        existingDeal.DealUrl = deal.DealUrl;
                        existingDeal.IsActive = true;
                        existingDeal.IsAvailable = true;
                        existingDeal.LastSyncedAt = DateTime.UtcNow;
                        existingDeal.StoreName = platformName;
                        existingDeal.PlatformId = platform.Id;
                        _dbContext.GameDeals.Update(existingDeal);
                    }
                    else
                    {
                        await _dbContext.GameDeals.AddAsync(new GameDeal
                        {
                            Id = Guid.NewGuid(),
                            Name = deal.Title,
                            Price = deal.SalePriceUsd,
                            OriginalPrice = deal.NormalPriceUsd,
                            DiscountPercent = deal.SavingsPercent > 0 ? (int)Math.Round(deal.SavingsPercent) : null,
                            ImageUrl = deal.ThumbUrl,
                            DealUrl = deal.DealUrl,
                            IsActive = true,
                            IsAvailable = true,
                            DisplayOrder = 0,
                            Source = DealSource.Live,
                            DealId = normalizedDealId,
                            StoreName = platformName,
                            PlatformId = platform.Id,
                            LastSyncedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Synced {Count} deals for {Platform}. Removed {Expired} expired.",
                    dealsList.Count, platformName, expiredDeals.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing live deals for platform {Platform}", platformName);
                throw;
            }
        }

        public async Task SyncAllLiveDealsAsync(CancellationToken cancellationToken = default)
        {
            foreach (var platform in new[] { "Steam", "GOG", "Epic Games" })
            {
                if (cancellationToken.IsCancellationRequested) break;
                await SyncLiveDealsForPlatformAsync(platform, cancellationToken);
                await Task.Delay(600, cancellationToken);
            }
        }

        public async Task<Guid> GetOrCreateGameDealIdAsync(string dealId, string storeName, CancellationToken cancellationToken = default)
        {
            var normalizedDealId = NormalizeDealId(dealId);

            var existing = await _dbContext.GameDeals
                .FirstOrDefaultAsync(d =>
                    d.Source == DealSource.Live && d.DealId != null && d.DealId.Trim() == normalizedDealId,
                    cancellationToken);

            if (existing != null) return existing.Id;

            try
            {
                await SyncLiveDealsForPlatformAsync(storeName, cancellationToken);
                existing = await _dbContext.GameDeals
                    .FirstOrDefaultAsync(d =>
                        d.Source == DealSource.Live && d.DealId != null && d.DealId.Trim() == normalizedDealId,
                        cancellationToken);
                if (existing != null) return existing.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync and find GameDeal for DealId {DealId}", dealId);
            }

            throw new InvalidOperationException($"GameDeal not found for DealId={dealId}. Sync runs on startup and every hour.");
        }

        private static string NormalizeDealId(string dealId)
        {
            var normalized = dealId.Trim();
            try
            {
                string prev;
                do
                {
                    prev = normalized;
                    normalized = Uri.UnescapeDataString(normalized);
                } while (normalized != prev && normalized.Contains('%'));
            }
            catch { }
            return normalized.Trim();
        }

        private sealed class CheapSharkDealResponse
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("dealID")]
            public string? DealId { get; set; }
            [JsonPropertyName("salePrice")]
            public decimal SalePrice { get; set; }
            [JsonPropertyName("normalPrice")]
            public decimal NormalPrice { get; set; }
            [JsonPropertyName("savings")]
            public decimal Savings { get; set; }
            [JsonPropertyName("thumb")]
            public string? Thumb { get; set; }
        }
    }
}
