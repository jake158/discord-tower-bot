using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Discord;
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly IAntivirusScanQueue _scanQueue;
    private readonly IServiceScopeFactory _scopeFactory;

    public MessageHandler(ILogger<MessageHandler> logger, IAntivirusScanQueue scanQueue, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scanQueue = scanQueue;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;

        _logger.LogDebug($"Processing message: {message}. Initializing scope...");

        using var scope = _scopeFactory.CreateScope();
        var dbManager = scope.ServiceProvider.GetRequiredService<BotDatabaseManager>();

        _logger.LogDebug($"Processing attachments/links...");

        int numberOfScans = QueueScans(message);


        if (numberOfScans > 0 && message.Channel is SocketGuildChannel guildChannel)
        {
            _logger.LogDebug($"Updating guild stats for message {message}...");
            await dbManager.UpdateGuildStatsAsync(guildChannel.Guild, numberOfScans);
        }
        else if (numberOfScans > 0)
        {
            _logger.LogDebug($"Updating user stats for message {message}...");
            await dbManager.UpdateUserStatsAsync(message.Author, numberOfScans);
        }
        _logger.LogDebug($"Message {message} processed");
    }

    private static IEnumerable<string> ExtractLinks(string content)
    {
        var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return from Match m in linkParser.Matches(content) select m.Value;
    }

    private int QueueScans(SocketUserMessage message)
    {
        int numberOfScans = 0;
        var tasks = new List<Task>();

        foreach (IAttachment attachment in message.Attachments)
        {
            numberOfScans++;
            tasks.Add(ProcessLinkAsync(message, new Uri(attachment.Url), true));
        }

        foreach (var link in ExtractLinks(message.Content))
        {
            try
            {
                numberOfScans++;
                tasks.Add(ProcessLinkAsync(message, new Uri(link), false));
            }
            catch (UriFormatException)
            {
                _logger.LogError($"Not a valid URI: {link}");
            }
        }

        // Fire and forget scans
        _ = Task.WhenAll(tasks);
        return numberOfScans;
    }

    private async Task ProcessLinkAsync(SocketUserMessage message, Uri link, bool forceFile)
    {
        try
        {
            var scanTask = await _scanQueue.QueueScanAsync(link, forceFile);
            var scanResult = await scanTask;

            _logger.LogDebug($"Scan result: {scanResult.Link}, Malware: {scanResult.IsMalware}, Suspicious: {scanResult.IsSuspicious}, ScannedLinkId: ${scanResult.ScannedLinkId}");

            if (scanResult.IsMalware || scanResult.IsSuspicious)
            {
                await HandleMalwareFoundAsync(message, scanResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error scanning link {link}: {ex.Message}");
        }
    }

    private async Task HandleMalwareFoundAsync(SocketUserMessage message, ScanResult scanResult)
    {
        _logger.LogInformation($"Malware found in {scanResult.Link}");
        await message.ReplyAsync($"Malware found in ${scanResult.Link}");

        using var scope = _scopeFactory.CreateScope();
        var dbManager = scope.ServiceProvider.GetRequiredService<BotDatabaseManager>();

        if (message.Channel is not SocketGuildChannel guildChannel)
        {
            return;
        }
        var guildSettings = await dbManager.GetGuildSettingsAsync(guildChannel.Guild);

        if (guildSettings == null)
        {
            _logger.LogError($"Could not get guild settings for Guild {guildChannel.Guild.Id}");
        }

        if (guildSettings?.AlertChannel != null
        && guildChannel.Guild.GetTextChannel((ulong)guildSettings.AlertChannel) is SocketTextChannel alertChannel)
        {
            await alertChannel.SendMessageAsync($"Warning! Malware sent in channel #{guildChannel.Name}");
        }
        await dbManager.UpdateGuildStatsAsync(guildChannel.Guild, 0, 1);
    }
}