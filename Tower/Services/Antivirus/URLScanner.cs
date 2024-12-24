using Microsoft.Extensions.Logging;
using Google.Cloud.WebRisk.V1;
using Tower.Services.Antivirus.Models;
using Tower.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace Tower.Services.Antivirus;
public class URLScanner
{
    private readonly ILogger<URLScanner> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WebRiskServiceClient _webRiskClient;

    public URLScanner(ILogger<URLScanner> logger, IServiceScopeFactory scopeFactory, WebRiskServiceClient webRiskClient)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _webRiskClient = webRiskClient;
    }

    public class URLScannerOptions
    {
        [Required]
        public string GoogleServiceAccountJson { get; set; } = "";
    }

    public async Task<ScanResult> ScanUrlAsync(Uri url, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Checking cache for URL: {url.AbsoluteUri}");

        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ScanResultCache>();

        var cachedResult = await cache.GetScanResultAsync(url, ResourceType.WebLink);

        if (cachedResult != null)
        {
            _logger.LogInformation($"Cache hit for URL: {url.AbsoluteUri}");
            return (ScanResult)cachedResult;
        }

        _logger.LogInformation($"Cache miss for URL: {url.AbsoluteUri}. Calling Web Risk API");

        var apiResponse = await CallWebRiskApiAsync(url, cancellationToken);

        _logger.LogInformation($"Saving scan result for URL: {url.AbsoluteUri} to cache");

        return await cache.SaveScanResultAsync(
            url: url,
            type: ResourceType.WebLink,
            scanSource: "Google Web Risk",
            isMalware: apiResponse.IsMalware,
            isSuspicious: apiResponse.IsSuspicious,
            expireTime: apiResponse.ExpireTime
        );
    }

    private async Task<ApiResponse> CallWebRiskApiAsync(Uri url, CancellationToken cancellationToken)
    {
        var threatTypes = new[] { ThreatType.Malware, ThreatType.SocialEngineering, ThreatType.UnwantedSoftware };

        SearchUrisResponse response;
        try
        {
            response = await _webRiskClient.SearchUrisAsync(
                uri: url.AbsoluteUri,
                threatTypes: threatTypes,
                cancellationToken
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling Web Risk API for URL: {url}");
            throw;
        }
        return ConstructApiResponse(response);
    }

    private static ApiResponse ConstructApiResponse(SearchUrisResponse response)
    {
        bool isMalware = false;
        bool isSuspicious = false;
        DateTimeOffset? expireTime = null;

        if (response.Threat != null)
        {
            var threat = response.Threat;

            if (threat.ThreatTypes.Contains(ThreatType.Malware) || threat.ThreatTypes.Contains(ThreatType.SocialEngineering))
            {
                isMalware = true;
            }
            if (threat.ThreatTypes.Contains(ThreatType.UnwantedSoftware))
            {
                isSuspicious = true;
            }
            if (threat.ExpireTime != null)
            {
                expireTime = threat.ExpireTime.ToDateTimeOffset();
            }
        }

        return new ApiResponse
        {
            IsMalware = isMalware,
            IsSuspicious = isSuspicious,
            ExpireTime = expireTime
        };
    }

    private readonly struct ApiResponse
    {
        public bool IsMalware { get; init; }
        public bool IsSuspicious { get; init; }
        public DateTimeOffset? ExpireTime { get; init; }
    }
}