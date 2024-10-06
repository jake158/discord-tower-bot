using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;
using Tower.Services.Antivirus.Models;

namespace Tower.Services.Discord;
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly IAntivirusScanQueue _scanQueue;

    public MessageHandler(ILogger<MessageHandler> logger, IAntivirusScanQueue scanQueue)
    {
        _logger = logger;
        _scanQueue = scanQueue;
    }

    public async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;

        _logger.LogDebug($"Processing message: {message}");

        foreach (IAttachment attachment in message.Attachments)
        {
            _logger.LogDebug($"Enqueuing attachment for scanning: {attachment.Url}");

            var scanTask = await _scanQueue.QueueScanAsync(new Uri(attachment.Url), true);
            var scanResult = await scanTask;

            _logger.LogDebug($"Scan result: {scanResult.Name}, Malware: {scanResult.IsMalware}, Suspicious: {scanResult.IsSuspicious}");

            if (scanResult.IsMalware || scanResult.IsSuspicious)
            {
                OnMalwareFound(scanResult, message);
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

                _logger.LogDebug($"Scan result: {scanResult.Name}, Malware: {scanResult.IsMalware}, Suspicious: {scanResult.IsSuspicious}");

                if (scanResult.IsMalware || scanResult.IsSuspicious)
                {
                    OnMalwareFound(scanResult, message);
                }
            }
            catch (UriFormatException)
            {
                _logger.LogError($"Not a valid URL: {link}");
            }
        }
    }

    private async void OnMalwareFound(ScanResult scanResult, SocketUserMessage message)
    {
        _logger.LogInformation($"Malware found in {scanResult.Name}");

        await message.ReplyAsync($"Malware found in ${scanResult.Name}");
    }
}