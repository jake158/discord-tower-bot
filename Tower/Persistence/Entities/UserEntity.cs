using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class UserEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }
    public bool Blacklisted { get; set; } = false;

    private ICollection<GuildEntity>? _ownedGuilds;
    private ICollection<UserOffenseEntity>? _offenses;

    public virtual ICollection<GuildEntity> OwnedGuilds => _ownedGuilds ??= new List<GuildEntity>();
    public virtual ICollection<UserOffenseEntity> Offenses => _offenses ??= new List<UserOffenseEntity>();
    public virtual UserStatsEntity Stats { get; set; } = null!;
}