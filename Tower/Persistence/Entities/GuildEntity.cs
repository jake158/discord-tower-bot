using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class GuildEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong GuildId { get; set; }
    [ForeignKey(nameof(Owner))]
    public ulong UserId { get; set; }

    public string Name { get; set; } = null!;
    public bool IsPremium { get; set; } = false;

    public virtual UserEntity Owner { get; set; } = null!;
    public virtual GuildStatsEntity Stats { get; set; } = null!;
    public virtual GuildSettingsEntity Settings { get; set; } = null!;
}
