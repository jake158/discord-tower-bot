using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class GuildSettingsEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [ForeignKey(nameof(Guild))]
    public ulong GuildId { get; set; }

    public bool IsScanEnabled { get; set; } = true;
    public ulong? AlertChannel { get; set; } = null;

    public GuildEntity Guild { get; set; } = null!;
}