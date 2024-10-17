using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class GuildStatsEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [ForeignKey(nameof(Guild))]
    public ulong GuildId { get; set; }

    public int TotalScans { get; set; } = 0;
    public int ScansToday { get; set; } = 0;
    public int MalwareFoundCount { get; set; } = 0;
    public DateTime JoinDate { get; set; }
    public DateTime? LastScanDate { get; set; } = null;

    public virtual GuildEntity Guild { get; set; } = null!;
}