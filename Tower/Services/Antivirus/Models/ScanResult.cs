
namespace Tower.Services.Antivirus.Models;
public readonly struct ScanResult
{
    public ScanResult(string link, int? scannedLinkId, bool isMalware, bool isSuspicious)
    {
        Link = link;
        ScannedLinkId = scannedLinkId;
        IsMalware = isMalware;
        IsSuspicious = isSuspicious;
    }

    public string Link { get; init; }
    public int? ScannedLinkId { get; init; }
    public bool IsMalware { get; init; }
    public bool IsSuspicious { get; init; }
}