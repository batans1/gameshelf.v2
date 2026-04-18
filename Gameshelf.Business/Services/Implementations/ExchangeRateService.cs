using System.Text.Json;
using GameShelf.Business.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameShelf.Business.Services.Implementations;

/// <summary>
/// Service for fetching and caching USD to EUR exchange rates from external API.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private const string CacheKey = "usd_to_eur_rate";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly LiveDealsOptions _options;

    public ExchangeRateService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ExchangeRateService> logger,
        IOptions<LiveDealsOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<decimal> GetUsdToEurRateAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue(CacheKey, out decimal cachedRate))
        {
            _logger.LogDebug("Using cached exchange rate: {Rate}", cachedRate);
            return cachedRate;
        }

        // If dynamic rate is disabled, use configured rate
        if (!_options.UseDynamicExchangeRate)
        {
            _logger.LogDebug("Dynamic exchange rate disabled, using configured rate: {Rate}", _options.UsdToEurRate);
            return _options.UsdToEurRate;
        }

        // Fetch from API
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            _logger.LogInformation("Fetching exchange rate from {Url}", _options.ExchangeRateApiUrl);
            var response = await client.GetAsync(_options.ExchangeRateApiUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                
                // Try to get EUR rate from response
                if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("EUR", out var eurRate))
                {
                    var rate = eurRate.GetDecimal();
                    _logger.LogInformation("Fetched exchange rate: 1 USD = {Rate} EUR", rate);
                    
                    // Cache the rate
                    var cacheDuration = TimeSpan.FromHours(_options.ExchangeRateCacheHours);
                    _cache.Set(CacheKey, rate, cacheDuration);
                    
                    return rate;
                }
            }
            
            _logger.LogWarning("Failed to fetch exchange rate. Status: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate from API");
        }

        // Fallback to configured rate
        _logger.LogWarning("Using fallback exchange rate: {Rate}", _options.UsdToEurRate);
        return _options.UsdToEurRate;
    }
}
