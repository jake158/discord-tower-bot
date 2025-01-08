using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tower.Persistence;

namespace Tower.Services.Discord;
public class BlacklistManager
{
    private readonly HashSet<ulong> _blacklistedUsers = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlacklistManager> _logger;

    public BlacklistManager(IServiceScopeFactory scopeFactory, ILogger<BlacklistManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        LoadBlacklistedUsers();
    }

    private void LoadBlacklistedUsers()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TowerDbContext>();

        var blacklistedUsers = db.Users
            .Where(u => u.Blacklisted == true)
            .Select(u => u.UserId);

        lock (_blacklistedUsers)
        {
            _blacklistedUsers.Clear();
            foreach (var userId in blacklistedUsers)
            {
                _blacklistedUsers.Add(userId);
            }
        }
        _logger.LogInformation("Blacklist loaded with {Count} users.", _blacklistedUsers.Count);
    }

    public bool IsBlacklisted(ulong userId)
    {
        lock (_blacklistedUsers)
        {
            return _blacklistedUsers.Contains(userId);
        }
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

    private async Task UpdateDatabaseBlacklistAsync(ulong userId, bool isBlacklisted)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbManager = scope.ServiceProvider.GetRequiredService<BotDatabaseManager>();

        await dbManager.UpdateUserBlacklistStatusAsync(userId, isBlacklisted);
        _logger.LogInformation("User {UserId} blacklist status updated to {Status}.", userId, isBlacklisted);
    }
}
