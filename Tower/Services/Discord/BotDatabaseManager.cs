using Discord.WebSocket;
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

    public async Task TrackUserAsync(SocketUser user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        _logger.LogInformation($"TrackUser: Tracking user {user.Id}...");

        var userEntity = await _db.Users.FindAsync(user.Id);
        if (userEntity != null)
        {
            _logger.LogInformation($"TrackUser: User {user.Id} already exists. Skipping.");
            return;
        }

        userEntity = new UserEntity
        {
            UserId = user.Id,
            Stats = new UserStatsEntity()
            {
                UserId = user.Id,
            }
        };

        _db.Users.Add(userEntity);
        await _db.SaveChangesAsync();
    }

    public async Task TrackGuildAsync(SocketGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        _logger.LogInformation($"TrackGuild: Tracking guild {guild.Id}...");

        var guildOwnerEntity = await _db.Users.FindAsync(guild.OwnerId);

        if (guildOwnerEntity == null)
        {
            guildOwnerEntity = new UserEntity()
            {
                UserId = guild.OwnerId,
            };

            _db.Users.Add(guildOwnerEntity);
        } else
        {
            _logger.LogInformation($"TrackGuild: Owner for guild {guild.Id} already exists in Users table...");
        }

        var guildEntity = await _db.Guilds.FindAsync(guild.Id);
        if (guildEntity == null)
        {
            guildEntity = new GuildEntity()
            {
                GuildId = guild.Id,
                UserId = guild.OwnerId,
                Name = guild.Name,
                Stats = new GuildStatsEntity()
                {
                    GuildId = guild.Id,
                    JoinDate = DateTime.UtcNow,
                },
                Settings = new GuildSettingsEntity()
                {
                    GuildId = guild.Id,
                }
            };

            guildOwnerEntity.OwnedGuilds.Add(guildEntity);
        } else {
            _logger.LogWarning($"TrackGuild is called for guild {guild.Id} but guild already exists in Guilds table");
        }
        await _db.SaveChangesAsync();
    }

    public async Task UpdateGuildStatsAsync(SocketGuild guild, int numberOfScans, int malwareFoundCount = 0)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        var guildStats = await _db.GuildStats.FindAsync(guild.Id);
        
        if (guildStats == null)
        {
            _logger.LogWarning($"UpdateGuildStats: Guild stats for guild {guild.Id} not found in db");

            await TrackGuildAsync(guild);
            guildStats = await _db.GuildStats.FindAsync(guild.Id);

            if (guildStats == null)
            {
                _logger.LogError($"UpdateGuildStats: Failed to get stats for guild {guild.Id} after calling TrackGuild");
                return;
            }
        }

        var dateNow = DateTime.Now;
        if (guildStats.LastScanDate != null && ((DateTime) guildStats.LastScanDate).Date == dateNow.AddDays(-1).Date)
        {
            guildStats.ScansToday = 0;
        }

        guildStats.TotalScans += numberOfScans;
        guildStats.ScansToday += numberOfScans;
        guildStats.MalwareFoundCount += malwareFoundCount;
        guildStats.LastScanDate = dateNow;

        await _db.SaveChangesAsync();
    }

    public async Task UpdateUserStatsAsync(SocketUser user, int numberOfScans)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        var userEntity = await _db.Users.FindAsync(user.Id);
        
        if (userEntity == null)
        {
            _logger.LogInformation($"UpdateUserStats: User stats for user {user.Id} not found in db");
            
            await TrackUserAsync(user);
            userEntity = await _db.Users.FindAsync(user.Id);

            if (userEntity == null)
            {
                _logger.LogError($"UpdateUserStats: Failed to get entity for user {user.Id} after calling TrackUser");
                return;
            }
        }
        var userStats = userEntity.Stats ??= new UserStatsEntity() { UserId = user.Id };

        var dateNow = DateTime.Now;
        if (userStats.LastScanDate != null && ((DateTime) userStats.LastScanDate).Date == dateNow.AddDays(-1).Date)
        {
            userStats.ScansToday = 0;
        }

        userStats.TotalScans += numberOfScans;
        userStats.ScansToday += numberOfScans;
        userStats.LastScanDate = dateNow;

        userEntity.Stats = userStats;
        await _db.SaveChangesAsync();
    }

    public async Task<GuildSettingsEntity?> GetGuildSettingsAsync(SocketGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        var guildSettings = await _db.GuildSettings.FindAsync(guild.Id);

        if (guildSettings == null)
        {
            _logger.LogInformation($"GetGuildSettings: Guild settings for guild {guild.Id} not found in db");

            await TrackGuildAsync(guild);
            guildSettings = await _db.GuildSettings.FindAsync(guild.Id);

            if (guildSettings == null)
            {
                _logger.LogError($"GetGuildSettings: Failed to get settings for guild {guild.Id} after calling TrackGuild");
            }
        }
        return guildSettings;
    }
}