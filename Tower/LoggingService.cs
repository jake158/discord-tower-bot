using Discord;
using Discord.WebSocket;

namespace Tower;
public class LoggingService
{
    public LoggingService(DiscordSocketClient client)
    {
        client.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}