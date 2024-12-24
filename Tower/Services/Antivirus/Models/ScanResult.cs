
namespace Tower.Services.Antivirus.Models;
public readonly struct ScanResult(string link, string scanSource, int? scannedLinkId, bool isMalware, bool isSuspicious)
{
    public string Link { get; init; } = link;
    public int? ScannedLinkId { get; init; } = scannedLinkId;
    public string ScanSource { get; init; } = scanSource;
    public bool IsMalware { get; init; } = isMalware;
    public bool IsSuspicious { get; init; } = isSuspicious;
}