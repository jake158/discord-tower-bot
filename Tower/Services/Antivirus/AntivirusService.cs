using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class AntivirusService(
    ILogger<AntivirusService> logger, 
    IAntivirusScanQueue scanQueue, 
    FileScanner fileScanner, 
    URLScanner urlScanner) : BackgroundService
{
    private readonly ILogger<AntivirusService> _logger = logger;
    private readonly IAntivirusScanQueue _scanQueue = scanQueue;
    private readonly FileScanner _fileScanner = fileScanner;
    private readonly URLScanner _urlScanner = urlScanner;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AntivirusService is starting...");

        while (!cancellationToken.IsCancellationRequested)
        {
            ScanRequest request = await _scanQueue.DequeueAsync(cancellationToken);

            try
            {
                ScanResult result = await ProcessScanRequestAsync(request, cancellationToken);
                request.TaskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred when processing ScanRequest");
                request.TaskCompletionSource.SetException(ex);
            }
        }

        _logger.LogInformation("AntivirusService is stopping...");
    }

    private async Task<ScanResult> ProcessScanRequestAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing ScanRequest: {request.Url.AbsoluteUri}");

        if (!(request.Url.Scheme == Uri.UriSchemeHttp || request.Url.Scheme == Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Not a valid Http/Https URL: {request.Url.AbsoluteUri}");
        }
        // TODO: Requesting header to see if downloadable or not

        if (request.IsFile == true || (request.IsFile == null && request.Url.IsFile))
        {
            _logger.LogInformation($"URL is a file: {request.Url.AbsoluteUri}");
            return await _fileScanner.ScanFileAtUrlAsync(request.Url, cancellationToken);
        }
        else
        {
            _logger.LogInformation($"URL is a web resource: {request.Url.AbsoluteUri}");
            return await _urlScanner.ScanUrlAsync(request.Url, cancellationToken);
        }
    }
}