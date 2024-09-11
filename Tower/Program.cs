using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Tower;
public class Program
{
    private static readonly IServiceProvider _serviceProvider = CreateProvider();

    static IServiceProvider CreateProvider()
    {
        var collection = new ServiceCollection()
            .AddSingleton<DiscordSocketClient>();

        return collection.BuildServiceProvider();
    }

    public static async Task Main()
    {
        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        client.Log += Log;

        var settings = Settings.Load();
        await client.LoginAsync(TokenType.Bot, settings.Token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
