using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tower.Persistence;
using Tower.Persistence.Entities;

namespace Tower.Services.Discord;
public class DenialManager
{
    private readonly HashSet<ulong> _blacklistedUsers = [];
    private readonly HashSet<ulong> _rateLimitedUsers = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DenialManager> _logger;

    public DenialManager(IServiceScopeFactory scopeFactory, ILogger<DenialManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        LoadUsers(_blacklistedUsers, u => u.Blacklisted, "Blacklist");
        LoadUsers(_rateLimitedUsers, u => u.RateLimited, "Rate limited list");
    }

    private void LoadUsers(HashSet<ulong> targetSet, Func<UserEntity, bool> predicate, string listName)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TowerDbContext>();

        var users = db.Users
            .Where(predicate)
            .Select(u => u.UserId);

        lock (targetSet)
        {
            targetSet.Clear();
            foreach (var userId in users)
            {
                targetSet.Add(userId);
            }
        }
        _logger.LogInformation("{ListName} loaded with {Count} users.", listName, targetSet.Count);
    }

    public bool IsBlacklisted(ulong userId)
    {
        lock (_blacklistedUsers)
        {
            return _blacklistedUsers.Contains(userId);
        }
    }

    public bool IsRateLimited(ulong userId)
    {
        lock (_rateLimitedUsers)
        {
            return _rateLimitedUsers.Contains(userId);
        }
    }

    public bool IsRestricted(ulong userId)
    {
        return IsRateLimited(userId) || IsBlacklisted(userId);
    }

    public async Task AddToBlacklistAsync(ulong userId)
    {
        lock (_blacklistedUsers)
        {
            _blacklistedUsers.Add(userId);
        }
        await UpdateDatabaseBlacklistAsync(userId, true);
    }

    public async Task RemoveFromBlacklistAsync(ulong userId)
    {
        lock (_blacklistedUsers)
        {
            _blacklistedUsers.Remove(userId);
        }
        await UpdateDatabaseBlacklistAsync(userId, false);
    }

    public async Task RateLimitAsync(ulong userId)
    {
        lock (_rateLimitedUsers)
        {
            _rateLimitedUsers.Add(userId);
        }
        await UpdateDatabaseRateLimitAsync(userId, true);
    }

    public async Task UnRateLimitAllAsync()
    {
        lock (_rateLimitedUsers)
        {
            _rateLimitedUsers.Clear();
        }
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TowerDbContext>();

        await db.Users
            .Where(u => u.RateLimited)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RateLimited, false));

        _logger.LogInformation("All users have been removed from the rate-limited list.");
    }

    private async Task UpdateUserAsync(ulong userId, Action<UserEntity> updateAction, string actionDescription)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbManager = scope.ServiceProvider.GetRequiredService<BotDatabaseManager>();
        var db = scope.ServiceProvider.GetRequiredService<TowerDbContext>();

        var user = await db.Users.FindAsync(userId)
                   ?? await dbManager.TrackUserAsync(userId, asTracking: true);

        updateAction(user);

        db.Users.Update(user);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} {ActionDescription} updated.", userId, actionDescription);
    }

    private async Task UpdateDatabaseBlacklistAsync(ulong userId, bool isBlacklisted)
    {
        await UpdateUserAsync(
            userId,
            user => user.Blacklisted = isBlacklisted,
            isBlacklisted ? "blacklist status" : "removed from blacklist"
        );
    }

    private async Task UpdateDatabaseRateLimitAsync(ulong userId, bool isRateLimited)
    {
        await UpdateUserAsync(
            userId,
            user => user.RateLimited = isRateLimited,
            isRateLimited ? "rate limited status" : "removed from rate limit"
        );
    }
}
