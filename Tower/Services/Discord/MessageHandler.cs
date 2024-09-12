using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Discord;
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(DiscordSocketClient client, ILogger<MessageHandler> logger)
    {
        _logger = logger;
        client.MessageReceived += HandleMessageAsync;
    }

    private async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;
        _logger.LogInformation($"Handling attachments for message {message}...");
    }
}
