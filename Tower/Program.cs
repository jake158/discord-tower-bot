using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Tower;
public class Program
{
    private static readonly IServiceProvider _services = CreateProvider();

    static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<LoggingService>()
            .AddSingleton<MessageHandler>();

        return collection.BuildServiceProvider();
    }

    public static async Task Main()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();

        _services.GetRequiredService<LoggingService>();
        _services.GetRequiredService<MessageHandler>();

        var settings = Settings.Load();
        await client.LoginAsync(TokenType.Bot, settings.Token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }
}
