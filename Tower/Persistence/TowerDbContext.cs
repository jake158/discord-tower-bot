using Microsoft.EntityFrameworkCore;
using Tower.Persistence.Entities;

namespace Tower.Persistence;
public class TowerDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<UserStatsEntity> UserStats { get; set; } = null!;
    public DbSet<UserOffenseEntity> UserOffenses { get; set; } = null!;
    public DbSet<GuildEntity> Guilds { get; set; } = null!;
    public DbSet<GuildStatsEntity> GuildStats { get; set; } = null!;
    public DbSet<GuildSettingsEntity> GuildSettings { get; set; } = null!;
    public DbSet<ScannedLinkEntity> ScannedLinks { get; set; } = null!;

    public TowerDbContext(DbContextOptions<TowerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserOffenseEntity>()
            .HasOne(uo => uo.Guild)
            .WithMany()
            .HasForeignKey(uo => uo.GuildId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
