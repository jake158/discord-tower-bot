using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class GuildEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong GuildId { get; set; }
    [ForeignKey(nameof(Owner))]
    public ulong UserID { get; set; }

    public string Name { get; set; } = null!;
    public bool IsPremium { get; set; }

    public UserEntity Owner { get; set; } = null!;
    public GuildStatsEntity Stats { get; set; } = null!;
    public GuildSettingsEntity Settings { get; set; } = null!;
}
