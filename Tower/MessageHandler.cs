using Discord.WebSocket;

namespace Tower;
public class MessageHandler
{
    public MessageHandler(DiscordSocketClient client)
    {
        client.MessageReceived += HandleMessageAsync;
    }

    private async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;

        await Task.Run(() =>
        {
            Console.WriteLine($"Handling attachments for message {message}...");
        });
    }
}
