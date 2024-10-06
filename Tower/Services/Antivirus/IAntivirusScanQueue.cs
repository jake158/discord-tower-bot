using Tower.Services.Antivirus.Models;

namespace Tower.Services.Antivirus;
public interface IAntivirusScanQueue
{
    Task<Task<ScanResult>> QueueScanAsync(Uri uri, bool? isFile = null);

    ValueTask<ScanRequest> DequeueAsync(CancellationToken cancellationToken);
}