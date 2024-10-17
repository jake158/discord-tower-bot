using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class UserOffenseEntity
{
    [Key]
    public int Id { get; set; }
    [ForeignKey(nameof(User))]
    public ulong UserId { get; set; }
    [ForeignKey(nameof(Guild))]
    public ulong? GuildId { get; set; }

    [StringLength(100)]
    public string OffenseDetails { get; set; } = null!;
    public DateTime OffenseDate { get; set; }

    public virtual UserEntity User { get; set; } = null!;
    public virtual GuildEntity? Guild { get; set; } = null;
}