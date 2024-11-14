using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class AntivirusScanQueue : IAntivirusScanQueue
{
    private readonly Channel<ScanRequest> _queue;

    public AntivirusScanQueue(IOptions<AntivirusScanQueueOptions> options)
    {
        var capacity = options.Value.Capacity;

        var boundedChannelOptions = new BoundedChannelOptions(capacity)
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
