using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tower.Persistence.Entities;
public class UserStatsEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [ForeignKey(nameof(User))]
    public ulong UserId { get; set; }

    public int TotalScans { get; set; } = 0;
    public int ScansToday { get; set; } = 0;
    public DateTime LastScanDate { get; set; }

    public UserEntity User { get; set; } = null!;
}