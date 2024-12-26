using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Tower.Jobs;

[DisallowConcurrentExecution]
public class ReportDailyStatsJob(
    ILogger<ReportDailyStatsJob> logger,
    DiscordSocketClient client,
    IOptions<ReportDailyStatsJob.ReportDailyStatsJobOptions> options) : IJob
{
    public static readonly JobKey Key = new("report-daily-stats-job", "reporting");

    private readonly ILogger<ReportDailyStatsJob> _logger = logger;
    private readonly DiscordSocketClient _client = client;
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
        if (context.RefireCount > 5)
        {
            _logger.LogError("Job execution failed after attempting to 5 refires times.");
            return;
        }

        _logger.LogInformation("Beginning reporting job...");

        try
        {
            var guildToNotify = _client.GetGuild(_options.GuildToNotifyId);
            var channelToNotify = guildToNotify.GetTextChannel(_options.ChannelToReportDailyStatsId);

            ArgumentNullException.ThrowIfNull(guildToNotify);
            ArgumentNullException.ThrowIfNull(channelToNotify);

            var dailyInfoEmbed = new EmbedBuilder()
                .WithTitle("Daily usage statistics")
                .WithTimestamp(DateTime.UtcNow)
                .WithDescription("Test...")
                .Build();

            await channelToNotify.SendMessageAsync(embed: dailyInfoEmbed);

            _logger.LogInformation("Reporting job completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fire reporting job");

            throw new JobExecutionException(msg: "", refireImmediately: true, cause: ex);
        }
    }
}
