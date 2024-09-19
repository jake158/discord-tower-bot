using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;

namespace Tower.Services.Discord;
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly AntivirusService _antivirusService;

    public MessageHandler(ILogger<MessageHandler> logger, AntivirusService antivirusService)
    {
        _logger = logger;
        _antivirusService = antivirusService;
        _antivirusService.OnMalwareFound += OnMalwareFound;
    }

    public Task HandleMessageAsync(SocketMessage messageParam)
    {
        // TODO: Submit ScanRequests instead of Enqueuing URLs
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return Task.CompletedTask;

        _logger.LogInformation($"Handling attachments for message '{message}', Author: {message.Author.Username}");

        foreach (IAttachment attachment in message.Attachments)
        {
            _logger.LogInformation($"Enqueuing attachment for scanning: {attachment.Url}");
            _antivirusService.EnqueueUrl(new Uri(attachment.Url));
        }

        var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var links = from Match m in linkParser.Matches(message.Content) select m.Value;

        foreach (var link in links)
        {
            try
            {
                _logger.LogInformation($"Enqueuing URL for scanning: {link}");
                _antivirusService.EnqueueUrl(new Uri(link));
            }
            catch (UriFormatException)
            {
                _logger.LogInformation($"Not a valid URL: {link}");
            }
        }
        return Task.CompletedTask;
    }

    private void OnMalwareFound(ScanResult result)
    {
        _logger.LogInformation($"Malware found in {result.Name}");
    }
}