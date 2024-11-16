using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tower.Persistence;
using Tower.Persistence.Entities;

namespace Tower.Services.Discord;
public class BotDatabaseManager
{
    private readonly ILogger<BotDatabaseManager> _logger;
    private readonly TowerDbContext _db;

    public BotDatabaseManager(ILogger<BotDatabaseManager> logger, TowerDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private async Task<UserEntity> EnsureUserExistsAsync(ulong userId, bool asTracking = true)
    {
        var userEntity = await (asTracking ? _db.Users : _db.Users.AsNoTracking())
            .Include(u => u.Stats)
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (userEntity != null) return userEntity;

        _logger.LogInformation($"Creating new user entity for UserId: {userId}");
        userEntity = new UserEntity
        {
            UserId = userId,
            Stats = new UserStatsEntity { UserId = userId }
        };

        _db.Users.Add(userEntity);
        await _db.SaveChangesAsync();

        return userEntity;
    }

    private async Task<GuildEntity> EnsureGuildExistsAsync(SocketGuild guild, bool asTracking = true)
    {
        var guildEntity = await (asTracking ? _db.Guilds : _db.Guilds.AsNoTracking())
            .Include(g => g.Stats)
            .Include(g => g.Settings)
            .SingleOrDefaultAsync(g => g.GuildId == guild.Id);

        if (guildEntity != null) return guildEntity;

        var ownerEntity = await EnsureUserExistsAsync(guild.OwnerId);
        await _db.Entry(ownerEntity).Collection(u => u.OwnedGuilds).LoadAsync();

        _logger.LogInformation($"Creating new guild entity for GuildId: {guild.Id}");
        guildEntity = new GuildEntity
        {
            GuildId = guild.Id,
            UserId = guild.OwnerId,
            Name = guild.Name,
            Stats = new GuildStatsEntity { GuildId = guild.Id, JoinDate = DateTime.UtcNow },
            Settings = new GuildSettingsEntity { GuildId = guild.Id }
        };

        ownerEntity.OwnedGuilds.Add(guildEntity);
        _db.Guilds.Add(guildEntity);
        await _db.SaveChangesAsync();

        return guildEntity;
    }

    public async Task TrackUserAsync(SocketUser user) => await EnsureUserExistsAsync(user.Id, asTracking: false);

    public async Task TrackGuildAsync(SocketGuild guild) => await EnsureGuildExistsAsync(guild, asTracking: false);

    public async Task UpdateGuildStatsAsync(SocketGuild guild, int numberOfScans, int malwareFoundCount = 0)
    {
        var guildEntity = await EnsureGuildExistsAsync(guild);
        var stats = guildEntity.Stats ??= new GuildStatsEntity { GuildId = guild.Id };

        var now = DateTime.UtcNow;
        if (stats.LastScanDate?.Date == now.AddDays(-1).Date)
        {
            stats.ScansToday = 0;
        }

        stats.TotalScans += numberOfScans;
        stats.ScansToday += numberOfScans;
        stats.MalwareFoundCount += malwareFoundCount;
        stats.LastScanDate = now;

        await _db.SaveChangesAsync();
    }

    public async Task UpdateUserStatsAsync(SocketUser user, int numberOfScans)
    {
        var userEntity = await EnsureUserExistsAsync(user.Id);
        var stats = userEntity.Stats ??= new UserStatsEntity { UserId = user.Id };

        var now = DateTime.UtcNow;
        if (stats.LastScanDate?.Date == now.AddDays(-1).Date)
        {
            stats.ScansToday = 0;
        }

        stats.TotalScans += numberOfScans;
        stats.ScansToday += numberOfScans;
        stats.LastScanDate = now;

        await _db.SaveChangesAsync();
    }

    public async Task<GuildSettingsEntity> GetGuildSettingsAsync(SocketGuild guild)
    {
        var guildEntity = await EnsureGuildExistsAsync(guild, false);
        return guildEntity.Settings;
    }

    public async Task SaveGuildSettingsAsync(GuildSettingsEntity guildSettings)
    {
        _db.Attach(guildSettings);
        _db.Entry(guildSettings).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        _logger.LogInformation($"Guild settings updated for GuildId: {guildSettings.GuildId}");
    }

    public async Task AddUserOffenseAsync(SocketUser user, string link, int scannedLinkId, SocketGuild? guild)
    {
        _logger.LogInformation($"Adding UserOffense: UserId={user.Id}, GuildId={guild?.Id}, ScannedLinkId={scannedLinkId}, Link={link}");

        var userEntity = await EnsureUserExistsAsync(user.Id);
        await _db.Entry(userEntity).Collection(u => u.Offenses).LoadAsync();

        var existingOffense = await _db.UserOffenses
                            .AsNoTracking()
                            .Where(o => o.UserId == user.Id)
                            .Where(o => o.GuildId == (guild != null ? guild.Id : null))
                            .Where(o => o.ScannedLinkId == scannedLinkId)
                            .FirstOrDefaultAsync();

        if (existingOffense != null)
        {
            _logger.LogInformation($"UserOffense already exists, returning: UserId={user.Id}, GuildId={guild?.Id}, ScannedLinkId={scannedLinkId}, Link={link}");
            return;
        }

        // Trim link to avoid malicious flooding of db with data
        link = link.Length > 300 ? link[..300] : link;

        var userOffense = new UserOffenseEntity()
        {
            UserId = user.Id,
            GuildId = guild?.Id,
            MaliciousLink = link,
            ScannedLinkId = scannedLinkId,
            OffenseDate = DateTime.UtcNow,
        };

        userEntity.Offenses.Add(userOffense);
        await _db.SaveChangesAsync();
    }
}
