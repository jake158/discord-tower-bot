using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tower.Persistence;

namespace Tower.Services.Discord.Commands;

[RequireTeam]
[Group("admin", "Admin commands")]
[DontAutoRegister]
public class AdminCommands(
    IHostApplicationLifetime lifetime,
    TowerDbContext db,
    DenialManager denialManager) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IHostApplicationLifetime _lifetime = lifetime;
    private readonly TowerDbContext _db = db;
    private readonly DenialManager _denialManager = denialManager;

    [SlashCommand("shutdown", "Shut down the bot")]
    public async Task ShutdownCommandAsync()
    {
        await RespondAsync("Shutting down...");
        _lifetime.StopApplication();
    }

    [SlashCommand("stats", "Browse usage statistics")]
    public async Task ViewStatsCommandAsync()
    {
        await DeferAsync();
        var (embed, components) = await AdminCommandViews.GetStatsViewAsync(_db, page: 0);
        await FollowupAsync(embed: embed, components: components);
    }

    [SlashCommand("guildstats", "Get usage statistics for a particular guild")]
    public async Task GuildStatsCommandAsync([Summary(name: "guild-id")] string guildIdString)
    {
        await DeferAsync();

        if (!ulong.TryParse(guildIdString, out ulong guildId))
        {
            await FollowupAsync($"Invalid Guild ID: {guildIdString}", ephemeral: true);
            return;
        }

        var guildStats = await _db.GuildStats
            .Include(gs => gs.Guild)
            .SingleOrDefaultAsync(gs => gs.GuildId == guildId);

        if (guildStats == null)
        {
            await FollowupAsync($"Guild with ID {guildId} not found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Statistics for guild: {guildStats.Guild.Name}")
            .AddField("Total Scans", guildStats.TotalScans)
            .AddField("Scans Today", guildStats.ScansToday)
            .AddField("Malware Found", guildStats.MalwareFoundCount)
            .AddField("Join Date", guildStats.JoinDate.ToString("g"))
            .AddField("Last Scan Date", guildStats.LastScanDate?.ToString("g") ?? "N/A")
            .WithColor(Color.LighterGrey);

        await FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("userstats", "Get usage statistics for a particular user")]
    public async Task UserStatsCommandAsync([Summary("user-id")] string userIdString)
    {
        await DeferAsync();

        if (!ulong.TryParse(userIdString, out ulong userId))
        {
            await FollowupAsync($"Invalid User ID: {userIdString}", ephemeral: true);
            return;
        }
        var userStats = await _db.UserStats
            .Include(us => us.User)
            .Include(us => us.User.Offenses)
            .Include(us => us.User.OwnedGuilds)
            .SingleOrDefaultAsync(us => us.UserId == userId);

        if (userStats == null)
        {
            await FollowupAsync($"User with ID {userId} not found.", ephemeral: true);
            return;
        }
        var offensesString = string.Join("\n", userStats.User.Offenses.Select(o => $"Link: {o.MaliciousLink} | Scanned Link Id: {o.ScannedLinkId}"));
        var ownedGuildsString = string.Join("\n", userStats.User.OwnedGuilds.Select(g => g.Name));

        var embed = new EmbedBuilder()
            .WithTitle($"Statistics for user with id: {userStats.UserId}")
            .AddField("Total Scans", userStats.TotalScans)
            .AddField("Scans Today", userStats.ScansToday)
            .AddField("Last Scan Date", userStats.LastScanDate?.ToString("g") ?? "N/A")
            .AddField("Offenses", offensesString.Length > 0 ? offensesString : "N/A")
            .AddField("Is Blacklisted", userStats.User.Blacklisted)
            .AddField("Owned Guilds", ownedGuildsString.Length > 0 ? ownedGuildsString : "N/A");

        await FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("alloffenses", "Browse all user offenses")]
    public async Task ViewAllOffensesCommandAsync()
    {
        await DeferAsync();
        var (embed, components) = await AdminCommandViews.GetOffensesViewAsync(_db, page: 0);
        await FollowupAsync(embed: embed, components: components);
    }

    [SlashCommand("useroffenses", "Get all offenses of a particular user")]
    public async Task GetUserOffensesCommandAsync([Summary("user-id")] string userIdString)
    {
        await DeferAsync();

        if (!ulong.TryParse(userIdString, out ulong userId))
        {
            await FollowupAsync($"Invalid User ID: {userIdString}", ephemeral: true);
            return;
        }

        var offensesExist = await _db.UserOffenses.AnyAsync(uo => uo.UserId == userId);
        if (!offensesExist)
        {
            await FollowupAsync($"No offenses found for user <@{userId}>.", ephemeral: true);
            return;
        }

        var (embed, components) = await AdminCommandViews.GetUserOffensesViewAsync(_db, userId, 0);

        await FollowupAsync(embed: embed, components: components);
    }

    [SlashCommand("scannedlink", "Get data of a particular scanned link")]
    public async Task ScannedLinkCommandAsync(int scannedLinkId)
    {
        await DeferAsync();
        var link = await _db.ScannedLinks.FindAsync(scannedLinkId);

        if (link == null)
        {
            await FollowupAsync($"Scanned link with ID {scannedLinkId} not found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Scanned Link ID: {link.Id}")
            .AddField("Link type:", $"`{link.Type}`")
            .AddField("Link hash:", $"`{link.LinkHash}`")
            .AddField("Is Malware:", $"`{link.IsMalware}`")
            .AddField("Is Suspicious:", $"`{link.IsSuspicious}`")
            .AddField("MD5:", $"`{link.MD5hash ?? "N/A"}`")
            .AddField("ExpireTime:", $"`{link.ExpireTime?.ToString() ?? "N/A"}`")
            .AddField("Scan Date:", $"`{link.ScannedAt:g}`");

        await FollowupAsync(embed: embed.Build());
    }

    [SlashCommand("deletescannedlink", "Delete data of a particular scanned link from the database")]
    public async Task DeleteScannedLinkCommandAsync(int scannedLinkId)
    {
        await DeferAsync();
        var link = await _db.ScannedLinks.FindAsync(scannedLinkId);

        if (link == null)
        {
            await FollowupAsync($"Scanned link with ID {scannedLinkId} not found.", ephemeral: true);
            return;
        }

        _db.ScannedLinks.Remove(link);
        await _db.SaveChangesAsync();

        await FollowupAsync($"Scanned link with ID {scannedLinkId} has been deleted.");
    }

    [SlashCommand("blacklist", "Change a user's blacklist status")]
    public async Task BlacklistCommandAsync([Summary("user-id")] string userIdString, bool isBlacklisted)
    {
        await DeferAsync();

        if (!ulong.TryParse(userIdString, out ulong userId))
        {
            await FollowupAsync($"Invalid User ID: {userIdString}", ephemeral: true);
            return;
        }
        if (isBlacklisted)
        {
            await _denialManager.AddToBlacklistAsync(userId);
        }
        else
        {
            await _denialManager.RemoveFromBlacklistAsync(userId);
        }

        await FollowupAsync($"User <@{userId}> has been {(isBlacklisted ? "blacklisted" : "unblacklisted")}.");
    }

    [SlashCommand("amend", "Remove a user offense")]
    public async Task AmendCommandAsync(int offenseId)
    {
        await DeferAsync();

        var offense = await _db.UserOffenses.FindAsync(offenseId);
        if (offense == null)
        {
            await FollowupAsync($"Offense with ID {offenseId} not found", ephemeral: true);
            return;
        }

        _db.UserOffenses.Remove(offense);
        await _db.SaveChangesAsync();

        await FollowupAsync($"Offense ID {offenseId} has been removed.");
    }

    [SlashCommand("amendall", "Remove all user offenses")]
    public async Task AmendAllCommandAsync([Summary("user-id")] string userIdString)
    {
        await DeferAsync();

        if (!ulong.TryParse(userIdString, out ulong userId))
        {
            await FollowupAsync($"Invalid User ID: {userIdString}", ephemeral: true);
            return;
        }

        var offenses = _db.UserOffenses.Where(uo => uo.UserId == userId);

        int count = await offenses.CountAsync();
        if (count == 0)
        {
            await FollowupAsync($"No offenses found for user <@{userId}>.", ephemeral: true);
            return;
        }

        _db.UserOffenses.RemoveRange(offenses);
        await _db.SaveChangesAsync();

        await FollowupAsync($"All offenses ({count}) for user <@{userId}> have been removed.");
    }

    [SlashCommand("premium", "Change a guild's premium status")]
    public async Task TogglePremiumCommandAsync([Summary("guild-id")] string guildIdString, bool isPremium)
    {
        await DeferAsync();

        if (!ulong.TryParse(guildIdString, out ulong guildId))
        {
            await FollowupAsync($"Invalid Guild ID: {guildIdString}", ephemeral: true);
            return;
        }

        var guild = await _db.Guilds.FindAsync(guildId);
        if (guild == null)
        {
            await FollowupAsync($"Guild with ID {guildId} not found.", ephemeral: true);
            return;
        }

        guild.IsPremium = isPremium;
        _db.Guilds.Update(guild);
        await _db.SaveChangesAsync();

        await FollowupAsync($"Guild **{guild.Name}** ({guildId}) premium status set to {isPremium}.");
    }
}
