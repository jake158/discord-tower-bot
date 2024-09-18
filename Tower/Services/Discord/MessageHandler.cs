using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Discord;
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(ILogger<MessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;

        _logger.LogInformation($"Handling attachments for message '{message}', Author: {message.Author.Username}");

        var attachmentData = "";
        foreach (IAttachment attachment in message.Attachments)
        {
            attachmentData += $"{attachment.Filename}:\nUrl: `{attachment.Url}`\nMIME type:{attachment.ContentType}\n";
        }

        await message.ReplyAsync(message.Attachments.Count > 0 ? attachmentData : "No attachments found");
    }
}
