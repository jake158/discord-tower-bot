using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Antivirus;
public class AntivirusService : BackgroundService
{
    private readonly ILogger<AntivirusService> _logger;
    private readonly IAntivirusScanQueue _scanQueue;
    private readonly FileScanner _fileScanner;
    private readonly URLScanner _urlScanner;

    public AntivirusService(ILogger<AntivirusService> logger, IAntivirusScanQueue scanQueue, FileScanner fileScanner, URLScanner urlScanner)
    {
        _logger = logger;
        _scanQueue = scanQueue;
        _fileScanner = fileScanner;
        _urlScanner = urlScanner;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AntivirusService is starting...");

        while (!cancellationToken.IsCancellationRequested)
        {
            ScanRequest request = await _scanQueue.DequeueAsync(cancellationToken);

            ScanResult result;
            try
            {
                result = await ProcessScanRequestAsync(request, cancellationToken);
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