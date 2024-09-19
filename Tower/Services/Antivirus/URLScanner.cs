using Microsoft.Extensions.Logging;

namespace Tower.Services.Antivirus;
public class URLScanner
{
    private readonly ILogger<URLScanner> _logger;
    // private readonly HttpClient _httpClient;
    // private readonly string _googleApiKey;
    // public URLScanner(ILogger<URLScanner> logger, HttpClient httpClient, Settings settings)
    public URLScanner(ILogger<URLScanner> logger)
    {
        _logger = logger;
        // _httpClient = httpClient;
        // _googleApiKey = settings.GoogleApiKey;
    }

    public async Task<ScanResult> ScanUrlAsync(Uri webUri)
    {
        if (webUri.IsFile)
        {
            throw new ArgumentException("The provided URI is a file, not a web URL", nameof(webUri));
        }

        _logger.LogInformation($"Scanning URL: {webUri.AbsoluteUri}");

        // string requestUrl = $"https://webrisk.googleapis.com/v1/uris:search?uri={webUri.AbsoluteUri}&threatTypes=MALWARE,UNWANTED_SOFTWARE,SOCIAL_ENGINEERING&key={_googleApiKey}";

        // var response = await _httpClient.GetAsync(requestUrl);
        // var content = await response.Content.ReadAsStringAsync();

        // bool isMalicious = content.Contains("threatTypes");

        bool isMalicious = false;

        return new ScanResult(webUri.AbsoluteUri, isMalware: isMalicious, isSuspicious: false);
    }
}