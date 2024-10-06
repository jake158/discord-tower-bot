
namespace Tower.Services.Antivirus.Models;
public readonly struct ScanResult
{
    public ScanResult(string name, bool isMalware, bool isSuspicious)
    {
        Name = name;
        IsMalware = isMalware;
        IsSuspicious = isSuspicious;
    }

    public string Name { get; init; }
    public bool IsMalware { get; init; }
    public bool IsSuspicious { get; init; }
}