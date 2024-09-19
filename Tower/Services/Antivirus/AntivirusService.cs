using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Tower.Services.Antivirus;
public class AntivirusService : BackgroundService
{
    private readonly ILogger<AntivirusService> _logger;
    private readonly FileScanner _fileScanner;
    private readonly URLScanner _urlScanner;
    private readonly ConcurrentQueue<Uri> _scanQueue = new();
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _isProcessingQueue = false;

    public event Action<ScanResult>? OnMalwareFound;

    public AntivirusService(ILogger<AntivirusService> logger, FileScanner fileScanner, URLScanner urlScanner)
    {
        _logger = logger;
        _fileScanner = fileScanner;
        _urlScanner = urlScanner;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AntivirusService is starting...");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AntivirusService is stopping...");
        }
    }

    public void EnqueueUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        _logger.LogInformation($"Enqueuing URL for scanning: {uri.AbsoluteUri}");
        _scanQueue.Enqueue(uri);

        if (!_isProcessingQueue)
        {
            _isProcessingQueue = true;
            _ = Task.Run(ProcessQueueAsync, _cancellationTokenSource.Token);
        }
    }

    private async Task ProcessQueueAsync()
    {
        _logger.LogInformation("ProcessQueueAsync triggered");

        while (!_scanQueue.IsEmpty && !_cancellationTokenSource.IsCancellationRequested)
        {
            if (_scanQueue.TryDequeue(out Uri? uri))
            {
                await ProcessUriAsync(uri);
            }
        }
        _isProcessingQueue = false;
    }

    private async Task ProcessUriAsync(Uri uri)
    {
        _logger.LogInformation($"Processing URI: {uri.AbsoluteUri}");

        if (uri.IsFile)
        {
            _logger.LogInformation($"URI is a file: {uri.AbsoluteUri}");
            var result = await _fileScanner.ScanFileAtUrlAsync(uri);
            HandleScanResult(result);
        }
        else
        {
            _logger.LogInformation($"URI is a web URL: {uri.AbsoluteUri}");
            var result = await _urlScanner.ScanUrlAsync(uri);
            HandleScanResult(result);
        }
    }

    private void HandleScanResult(ScanResult result)
    {
        if (result.IsMalware || result.IsSuspicious)
        {
            _logger.LogInformation($"Malicious or suspicious content found in: {result.Name}");
            OnMalwareFound?.Invoke(result);
        }
        else
        {
            _logger.LogInformation($"Scan completed for {result.Name}: No threats detected.");
        }
    }
}