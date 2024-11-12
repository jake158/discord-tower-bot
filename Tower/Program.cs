using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Persistence;
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

            config.AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                config.AddUserSecrets<Program>(true);
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

            services.AddDbContext<TowerDbContext>(options =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                var password = config["SqlServerPassword"] ?? Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
                var sqlServerHost = Environment.GetEnvironmentVariable("SQL_SERVER_HOST") ?? "localhost";
                var sqlServerPort = config["SqlServerPort"] ?? Environment.GetEnvironmentVariable("SQL_SERVER_PORT") ?? "1433";

                var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    Password = password,
                    DataSource = $"{sqlServerHost},{sqlServerPort}"
                };
                var connection = conStrBuilder.ConnectionString;

                options.UseSqlServer(connection);
            });


            int antivirusQueueCapacity = config.GetValue<int>("Antivirus:QueueCapacity");
            Console.WriteLine($"Antivirus queue capacity: {antivirusQueueCapacity}");

            services
                .AddSingleton<IAntivirusScanQueue>(new AntivirusScanQueue(antivirusQueueCapacity))
                .AddScoped<ScanResultCache>()
                .AddSingleton<FileScanner>()
                .AddSingleton<URLScanner>();

            // services
            //     .AddOptions<URLScanner.URLScannerOptions>()
            //     .Configure<IConfiguration>((options, config) =>
            //     {
            //         var googleApiKey = config["GoogleApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            //         ArgumentNullException.ThrowIfNull(googleApiKey, nameof(googleApiKey));
            //         options.GoogleApiKey = googleApiKey;
            //     });

            services.AddHostedService<AntivirusService>();


            var discordSocketConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            var interactionServiceConfig = new InteractionServiceConfig()
            {
                UseCompiledLambda = true
            };

            services
                .AddScoped<BotDatabaseManager>()
                .AddSingleton(discordSocketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<DiscordLogHandler>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<InteractionService>(serviceProvider =>
                {
                    var client = serviceProvider.GetRequiredService<DiscordSocketClient>();
                    return new InteractionService(client.Rest, interactionServiceConfig);
                });

            services
                .Configure<FileScanner.FileScannerOptions>(config.GetSection("AntivirusServer"))
                .AddOptions<BotService.BotServiceOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    var token = config["DiscordToken"] ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");
                    ArgumentNullException.ThrowIfNull(token, nameof(token));
                    options.Token = token;
                    options.TestGuildID = config.GetValue<ulong?>("Discord:TestGuildID");
                });

            services.AddHostedService<BotService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        })
        .Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TowerDbContext>();
            db.Database.Migrate();
        }

        await host.RunAsync();
    }
}
