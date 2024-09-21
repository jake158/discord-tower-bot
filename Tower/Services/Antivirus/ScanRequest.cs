
namespace Tower.Services.Antivirus;
public readonly struct ScanRequest
{
    public Uri Url { get; init; }
    public bool? IsFile { get; init; }
    public TaskCompletionSource<ScanResult> TaskCompletionSource { get; init; }

    public ScanRequest(Uri url, bool? isFile, TaskCompletionSource<ScanResult> taskCompletionSource)
    {
        Url = url;
        IsFile = isFile;
        TaskCompletionSource = taskCompletionSource;
    }
}