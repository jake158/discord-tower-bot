using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class AntivirusScanQueue : IAntivirusScanQueue
{
    private readonly ILogger<AntivirusScanQueue> _logger;
    private readonly Channel<ScanRequest> _queue;
    private readonly int _capacity;

    public AntivirusScanQueue(ILogger<AntivirusScanQueue> logger, IOptions<AntivirusScanQueueOptions> options)
    {
        _logger = logger;
        _capacity = options.Value.Capacity;

        var boundedChannelOptions = new BoundedChannelOptions(_capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<ScanRequest>(boundedChannelOptions);
    }

    public class AntivirusScanQueueOptions
    {
        [Required]
        public int Capacity { get; set; } = 5;
    }

    public async Task<Task<ScanResult>> QueueScanAsync(Uri url, bool? isFile = null)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }
        var currentCapacity = _queue.Reader.Count;

        if (currentCapacity >= _capacity)
        {
            _logger.LogCritical("ScanQueue has reached full capacity: {CurrentCapacity}/{MaxCapacity}. Attempting to enqueue URL: {Url}", currentCapacity, _capacity, url);
        }
        else if (currentCapacity >= Math.Floor(_capacity * 0.8))
        {
            _logger.LogWarning("ScanQueue is nearing full capacity (80%): {CurrentCapacity}/{MaxCapacity}", currentCapacity, _capacity);
        }
        else if (currentCapacity >= Math.Floor(_capacity * 0.5))
        {
            _logger.LogWarning("ScanQueue is at 50% capacity: {CurrentCapacity}/{MaxCapacity}", currentCapacity, _capacity);
        }

        _logger.LogDebug("Attempting to enqueue URL: {Url}. Current queue usage: {CurrentCapacity}/{MaxCapacity}", url, currentCapacity, _capacity);

        var tcs = new TaskCompletionSource<ScanResult>();
        var scanRequest = new ScanRequest(url, isFile, tcs);

        await _queue.Writer.WriteAsync(scanRequest);

        return tcs.Task;
    }

    public async ValueTask<ScanRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}
