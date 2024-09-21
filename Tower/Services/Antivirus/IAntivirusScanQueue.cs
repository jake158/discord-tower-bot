
namespace Tower.Services.Antivirus;
public interface IAntivirusScanQueue
{
    Task<ScanResult> QueueScanAsync(Uri uri, bool? isFile = null);

    ValueTask<ScanRequest> DequeueAsync(CancellationToken cancellationToken);
}