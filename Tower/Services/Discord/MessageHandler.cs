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

        _logger.LogDebug($"Scope initialized for message: {message}. Processing attachments/links...");

        int numberOfScans = 0;

        foreach (IAttachment attachment in message.Attachments)
        {
            _logger.LogDebug($"Enqueuing attachment for scanning: {attachment.Url}");

            var scanTask = await _scanQueue.QueueScanAsync(new Uri(attachment.Url), true);
            var scanResult = await scanTask;
            numberOfScans++;

            _logger.LogDebug($"Scan result: {scanResult.Name}, Malware: {scanResult.IsMalware}, Suspicious: {scanResult.IsSuspicious}");

            if (scanResult.IsMalware || scanResult.IsSuspicious)
            {
                OnMalwareFoundAsync(dbManager, scanResult, message);
            }
        }

        var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var links = from Match m in linkParser.Matches(message.Content) select m.Value;

        foreach (var link in links)
        {
            try
            {
                _logger.LogDebug($"Enqueuing URL for scanning: {link}");

                var scanTask = await _scanQueue.QueueScanAsync(new Uri(link));
                var scanResult = await scanTask;
                numberOfScans++;

                _logger.LogDebug($"Scan result: {scanResult.Name}, Malware: {scanResult.IsMalware}, Suspicious: {scanResult.IsSuspicious}");

                if (scanResult.IsMalware || scanResult.IsSuspicious)
                {
                    OnMalwareFoundAsync(dbManager, scanResult, message);
                }
            }
            catch (UriFormatException)
            {
                _logger.LogError($"Not a valid URL: {link}");
            }
        }

        if (message.Channel is SocketGuildChannel guildChannel && numberOfScans > 0)
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

    private async void OnMalwareFoundAsync(BotDatabaseManager dbManager, ScanResult scanResult, SocketUserMessage message)
    {
        _logger.LogInformation($"Malware found in {scanResult.Name}");
        await message.ReplyAsync($"Malware found in ${scanResult.Name}");

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
        && guildChannel.Guild.GetTextChannel((ulong) guildSettings.AlertChannel) is SocketTextChannel alertChannel)
        {
            await alertChannel.SendMessageAsync($"Warning! Malware sent in channel #{guildChannel.Name}");
        }
        await dbManager.UpdateGuildStatsAsync(guildChannel.Guild, 0, 1);
    }
}