using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Tower.Persistence.Entities;
public enum ResourceType
{
    Unknown = 0,
    File = 1,
    WebLink = 2
}

[Index(nameof(LinkHash), IsUnique = true)]
public class ScannedLinkEntity
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(maximumLength: 64, MinimumLength = 64)]
    public string LinkHash { get; set; } = null!;

    public ResourceType Type { get; set; } = ResourceType.Unknown;

    public bool IsMalware { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime ScannedAt { get; set; }

    [StringLength(maximumLength: 32, MinimumLength = 32)]
    // ResourceType.File: Used for downloaded file checksums
    public string? MD5hash { get; set; } = null!;

    // ResourceType.WebLink: Used for Google Web Risk API caching
    public DateTimeOffset? ExpireTime { get; set; }
}
