using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Services.Configuration;
using Tower.Services.Discord;

namespace Tower;
internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(Settings.Load());

                var config = new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                };

                services
                    .AddSingleton(config)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<LoggingService>()
                    .AddSingleton<MessageHandler>();

                services.AddHostedService<BotService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        await host.RunAsync();
    }
}
