using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;

namespace Tower.Services.Discord;
public partial class MessageHandler(
    ILogger<MessageHandler> logger,
    IAntivirusScanQueue scanQueue,
    IServiceScopeFactory scopeFactory,
    DenialManager denialManager,
    MalwareHandler malwareHandler)
{
    private readonly ILogger<MessageHandler> _logger = logger;
    private readonly IAntivirusScanQueue _scanQueue = scanQueue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly DenialManager _denialManager = denialManager;
    private readonly MalwareHandler _malwareHandler = malwareHandler;

    [GeneratedRegex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.IgnoreCase)]
    private static partial Regex GetLinkParser();

    public async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message
            || message.Author.IsBot
            || _denialManager.IsRestricted(message.Author.Id)) return;

        _logger.LogDebug($"Processing message: {message}.");

        using var scope = _scopeFactory.CreateScope();
        var dbManager = scope.ServiceProvider.GetRequiredService<BotDatabaseManager>();

        var guildChannel = message.Channel as SocketGuildChannel;
        bool processAttachments = true;

        if (guildChannel != null)
        {
            // TODO: measure perf
            var guildSettings = await dbManager.GetGuildSettingsAsync(guildChannel.Guild);
            processAttachments = guildSettings.IsScanEnabled;
            _logger.LogDebug($"ProcessAttachments: {processAttachments}");
        }
        int numberOfScans = processAttachments ? QueueScans(message) : 0;


        if (numberOfScans > 0 && guildChannel != null)
        {
            _logger.LogDebug($"Updating guild stats for message {message}...");

            await dbManager.UpdateGuildStatsAsync(
                guild: guildChannel.Guild,
                numberOfScans: numberOfScans
                );
        }
        else if (numberOfScans > 0)
        {
            _logger.LogDebug($"Updating user stats for message {message}...");

            await dbManager.UpdateUserStatsAsync(
                user: message.Author,
                numberOfScans: numberOfScans
                );
        }
        _logger.LogDebug($"Message {message} processed");
    }

    private static IEnumerable<string> ExtractLinks(string content)
    {
        return from Match m in GetLinkParser().Matches(content) select m.Value;
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

            if (scanResult.IsMalware)
            {
                await _malwareHandler.HandleMalwareFoundAsync(message, scanResult);
            }
            else if (scanResult.IsSuspicious)
            {
                await _malwareHandler.HandleSuspiciousFoundAsync(message, scanResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error scanning link {link}: {ex.Message}");
        }
    }
}