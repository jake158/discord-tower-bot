using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Services.Antivirus;
using Tower.Services.Discord;

namespace Tower;
internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddCommandLine(args);
        })
        .ConfigureAppConfiguration((context, config) =>
        {
            var env = context.HostingEnvironment;
            Console.WriteLine($"Running in environment: {env.EnvironmentName}");

            if (env.IsDevelopment())
            {
                config.AddUserSecrets<Program>();
            }

            // if (env.IsProduction())
            // {
            //     var builtConfig = config.Build();
            //     var keyVaultEndpoint = new Uri(builtConfig["KeyVault:VaultUri"]);
            //     var credential = new DefaultAzureCredential();
            //     config.AddAzureKeyVault(keyVaultEndpoint, credential);
            // }
        })
        .ConfigureServices((context, services) =>
        {
            var config = context.Configuration;
            int antivirusQueueCapacity = config.GetValue<int>("Antivirus:QueueCapacity");
            Console.WriteLine($"Antivirus queue capacity: {antivirusQueueCapacity}");

            services
                .AddSingleton<IAntivirusScanQueue>(new AntivirusScanQueue(antivirusQueueCapacity))
                .AddSingleton<FileScanner>()
                .AddSingleton<URLScanner>();

            services.AddHostedService<AntivirusService>();


            var discordSocketConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            services
                .AddSingleton(discordSocketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<DiscordLogHandler>()
                .AddSingleton<MessageHandler>()
                .AddOptions<BotService.Settings>()
                .Bind(config.GetRequiredSection("Discord"));

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
