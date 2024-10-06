using System.Threading.Channels;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public class AntivirusScanQueue : IAntivirusScanQueue
{
    private readonly Channel<ScanRequest> _queue;

    public AntivirusScanQueue(int capacity = 2)
    {
        // Capacity should be set based on the expected application load and
        // number of concurrent threads accessing the queue.            
        // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
        // which completes only when space became available. This leads to backpressure,
        // in case too many publishers/calls start accumulating.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<ScanRequest>(options);
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
