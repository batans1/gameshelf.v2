namespace GameShelf.Business.Services;

/// <summary>
/// Configuration for live deals (e.g. USD to EUR conversion rate).
/// </summary>
public class LiveDealsOptions
{
    public const string SectionName = "LiveDeals";

    /// <summary>
    /// Exchange rate: 1 USD = this many EUR. Default 0.92.
    /// Used as fallback if dynamic rate fetching fails.
    /// </summary>
    public decimal UsdToEurRate { get; set; } = 0.92m;

    /// <summary>
    /// Whether to fetch exchange rate dynamically from external API.
    /// Default: false (use configured rate).
    /// </summary>
    public bool UseDynamicExchangeRate { get; set; } = false;

    /// <summary>
    /// Exchange rate API endpoint. Default uses exchangerate-api.com free tier.
    /// </summary>
    public string ExchangeRateApiUrl { get; set; } = "https://api.exchangerate-api.com/v4/latest/USD";

    /// <summary>
    /// Cache duration for exchange rate in hours. Default: 24 hours.
    /// </summary>
    public int ExchangeRateCacheHours { get; set; } = 24;
}
