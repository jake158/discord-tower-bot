using Discord.WebSocket;

namespace Tower.Services.Discord;
public class MessageHandler
{
    public MessageHandler(DiscordSocketClient client)
    {
        client.MessageReceived += HandleMessageAsync;
    }

    private async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;
        Console.WriteLine($"Handling attachments for message {message}...");
    }
}
