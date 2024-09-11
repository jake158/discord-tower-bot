using Discord;
using Discord.WebSocket;

namespace Tower;
public class Program
{
    private static DiscordSocketClient _client = new();

    public static async Task Main()
    {
        _client.Log += Log;

        var settings = Settings.Load();

        await _client.LoginAsync(TokenType.Bot, settings.Token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }


    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
