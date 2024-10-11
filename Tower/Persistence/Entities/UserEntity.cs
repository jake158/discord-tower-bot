using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class UserEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong UserId { get; set; }
    [StringLength(32)]
    public string Username { get; set; } = null!;
    public bool Blacklisted { get; set; } = false;

    private ICollection<GuildEntity>? _guilds;
    private ICollection<UserOffenseEntity>? _offenses;

    public ICollection<GuildEntity> Guilds => _guilds ??= new List<GuildEntity>();
    public ICollection<UserOffenseEntity> Offenses => _offenses ??= new List<UserOffenseEntity>();
    public UserStatsEntity? Stats { get; set; }
}