using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameShelf.Web.HealthChecks;


/// Health check for external API (CheapShark) availability.

public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalApiHealthCheck> _logger;

    public ExternalApiHealthCheck(IHttpClientFactory httpClientFactory, ILogger<ExternalApiHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            // ping to CheapShark API
            var response = await client.GetAsync("https://www.cheapshark.com/api/1.0/stores", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("External API (CheapShark) is accessible");
            }
            
            return HealthCheckResult.Degraded($"External API returned status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External API health check failed");
            return HealthCheckResult.Unhealthy("External API is not accessible", ex);
        }
    }
}

