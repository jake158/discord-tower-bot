using Discord;
using Microsoft.EntityFrameworkCore;
using Tower.Persistence;

namespace Tower.Services.Discord.Commands;
public class AdminCommandViews
{
    public static async Task<(Embed, MessageComponent)> GetStatsViewAsync(TowerDbContext db, int page)
    {
        int pageSize = 6;
        int position = page * pageSize;

        var topGuildsPage = await db.GuildStats
            .OrderByDescending(gs => gs.ScansToday)
            .Skip(position)
            .Take(pageSize)
            .ToListAsync();

        var topUsersPage = await db.UserStats
            .OrderByDescending(us => us.ScansToday)
            .Skip(position)
            .Take(pageSize)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle("Usage Statistics")
            .WithDescription("Top guilds and users by scans performed today.\n")
            .WithTimestamp(DateTime.UtcNow);

        int rank = position + 1;

        string rankAndId = "";
        string totalScans = "";
        string scansToday = "";

        foreach (var gs in topGuildsPage)
        {
            rankAndId += $"`{rank} | {gs.GuildId}`\n";
            totalScans += $"`{gs.TotalScans}`\n";
            scansToday += $"`{gs.ScansToday}`\n";
            rank++;
        }
        embed.AddField("Rank | Guild Id", value: rankAndId.Length > 0 ? rankAndId : "N/A", inline: true);
        embed.AddField("Total Scans", value: totalScans.Length > 0 ? totalScans : "N/A", inline: true);
        embed.AddField("Scans Today", value: scansToday.Length > 0 ? scansToday : "N/A", inline: true);

        rank = position + 1;

        rankAndId = "";
        totalScans = "";
        scansToday = "";

        foreach (var us in topUsersPage)
        {
            rankAndId += $"`{rank} | {us.UserId}`\n";
            totalScans += $"`{us.TotalScans}`\n";
            scansToday += $"`{us.ScansToday}`\n";
            rank++;
        }
        embed.AddField("Rank | User Id", value: rankAndId.Length > 0 ? rankAndId : "N/A", inline: true);
        embed.AddField("Total Scans", value: totalScans.Length > 0 ? totalScans : "N/A", inline: true);
        embed.AddField("Scans Today", value: scansToday.Length > 0 ? scansToday : "N/A", inline: true);

        var components = new ComponentBuilder()
            .WithButton("Prev", customId: $"statsview:prev,{page}", disabled: page == 0)
            .WithButton("Next", customId: $"statsview:next,{page}", disabled: (topGuildsPage.Count == 0) && (topUsersPage.Count == 0));

        return (embed.Build(), components.Build());
    }

    public static async Task<(Embed, MessageComponent)> GetOffensesViewAsync(TowerDbContext db, int page)
    {
        int pageSize = 3;
        int position = page * pageSize;

        var offensesPage = await db.UserOffenses
            .Include(uo => uo.Guild)
            .OrderByDescending(uo => uo.OffenseDate)
            .Skip(position)
            .Take(pageSize)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle("User Offenses")
            .WithDescription($"Page {page + 1}");

        string offenseIds = "";
        string userIds = "";
        string scannedLinkIds = "";

        foreach (var offense in offensesPage)
        {
            offenseIds += $"`{offense.Id}`\n";
            userIds += $"`{offense.UserId}`\n";
            scannedLinkIds += $"`{offense.ScannedLinkId}`\n";
        }
        embed.AddField("Offense Id", offenseIds.Length > 0 ? offenseIds : "N/A", inline: true);
        embed.AddField("User Id", userIds.Length > 0 ? userIds : "N/A", inline: true);
        embed.AddField("Scanned Link Id", scannedLinkIds.Length > 0 ? scannedLinkIds : "N/A", inline: true);

        var components = new ComponentBuilder()
            .WithButton("Previous", customId: $"alloffensesview:prev,{page}", disabled: page == 0)
            .WithButton("Next", customId: $"alloffensesview:next,{page}", disabled: offensesPage.Count == 0);

        return (embed.Build(), components.Build());
    }

    public static async Task<(Embed, MessageComponent)> GetUserOffensesViewAsync(TowerDbContext db, ulong userId, int page)
    {
        int pageSize = 3;
        int position = page * pageSize;

        var offensesPage = await db.UserOffenses
            .Include(uo => uo.Guild)
            .Where(uo => uo.UserId == userId)
            .OrderByDescending(uo => uo.OffenseDate)
            .Skip(position)
            .Take(pageSize)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle($"User Offenses for user {userId}")
            .WithDescription($"Page {page + 1}");

        foreach (var offense in offensesPage)
        {
            string description = $"**Guild:** `{(offense.Guild != null ? offense.Guild.Name : "N/A")}`\n" +
                                 $"**Guild Id:** `{(offense.GuildId != null ? offense.GuildId : "N/A")}`\n" +
                                 $"**Link:** {offense.MaliciousLink}\n" +
                                 $"**Scanned link Id:** `{offense.ScannedLinkId}`\n" +
                                 $"**Date:** `{offense.OffenseDate:g}`";

            embed.AddField($"Offense ID: `{offense.Id}`", description);
        }

        var components = new ComponentBuilder()
            .WithButton("Previous", customId: $"useroffensesview:{userId},prev,{page}", disabled: page == 0)
            .WithButton("Next", customId: $"useroffensesview:{userId},next,{page}", disabled: offensesPage.Count == 0);

        return (embed.Build(), components.Build());
    }
}