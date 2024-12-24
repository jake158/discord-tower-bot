
namespace Tower.Services.Antivirus.Models;
public readonly struct ScanRequest(Uri url, bool? isFile, TaskCompletionSource<ScanResult> taskCompletionSource)
{
    public Uri Url { get; init; } = url;
    public bool? IsFile { get; init; } = isFile;
    public TaskCompletionSource<ScanResult> TaskCompletionSource { get; init; } = taskCompletionSource;
}