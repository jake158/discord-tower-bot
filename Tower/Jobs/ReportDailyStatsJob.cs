using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Tower.Persistence;

namespace Tower.Jobs;

[DisallowConcurrentExecution]
public class ReportDailyStatsJob(
    ILogger<ReportDailyStatsJob> logger,
    DiscordSocketClient client,
    TowerDbContext db,
    IOptions<ReportDailyStatsJob.ReportDailyStatsJobOptions> options) : IJob
{
    public static readonly JobKey Key = new("report-daily-stats-job", "reporting");
    public static readonly int RefireCount = 5;

    private readonly ILogger<ReportDailyStatsJob> _logger = logger;
    private readonly DiscordSocketClient _client = client;
    private readonly TowerDbContext _db = db;
    private readonly ReportDailyStatsJobOptions _options = options.Value;

    public class ReportDailyStatsJobOptions
    {
        [Required]
        public ulong GuildToNotifyId { get; set; }
        [Required]
        public ulong ChannelToReportDailyStatsId { get; set; }
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (context.RefireCount > RefireCount)
        {
            _logger.LogError($"Job execution failed after attempting {RefireCount} refires.");
            return;
        }

        _logger.LogInformation("Beginning reporting job...");

        try
        {
            var guildStatsSum = await _db.GuildStats.SumAsync(gs => gs.ScansToday);
            var userStatsSum = await _db.UserStats.SumAsync(us => us.ScansToday);

            var guildToNotify = _client.GetGuild(_options.GuildToNotifyId);
            var channelToNotify = guildToNotify.GetTextChannel(_options.ChannelToReportDailyStatsId);

            ArgumentNullException.ThrowIfNull(guildToNotify);
            ArgumentNullException.ThrowIfNull(channelToNotify);

            var dailyInfoEmbed = new EmbedBuilder()
                .WithTitle("Daily usage statistics")
                .WithTimestamp(DateTime.UtcNow)
                .AddField("Guild scans today", guildStatsSum, inline: false)
                .AddField("User scans today", userStatsSum, inline: false)
                .Build();

            await channelToNotify.SendMessageAsync(embed: dailyInfoEmbed);

            _logger.LogInformation("Reporting job completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute reporting job");

            throw new JobExecutionException(msg: "ReportDailyStatsJob failed", refireImmediately: true, cause: ex);
        }
    }
}
