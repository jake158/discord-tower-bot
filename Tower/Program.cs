using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;
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
            services
                .AddSingleton<FileScanner>()
                .AddSingleton<URLScanner>()
                .AddSingleton<AntivirusService>();

            services.AddHostedService(provider => provider.GetRequiredService<AntivirusService>());


            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            services
                .AddSingleton(Settings.Load())
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<DiscordLogHandler>()
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
