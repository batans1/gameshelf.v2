namespace GameShelf.Business.Services.Interfaces;

/// <summary>
/// Service for fetching and caching USD to EUR exchange rates.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the current USD to EUR exchange rate.
    /// Returns cached value if available and fresh, otherwise fetches from API.
    /// </summary>
    Task<decimal> GetUsdToEurRateAsync(CancellationToken cancellationToken = default);
}
