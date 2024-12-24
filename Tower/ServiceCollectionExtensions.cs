using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.WebRisk.V1;
using Grpc.Auth;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tower.Persistence;
using Tower.Services.Antivirus;
using Tower.Services.Discord;

namespace Tower;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
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
            options.UseSqlServer(conStrBuilder.ConnectionString);
        });

        return services;
    }

    public static IServiceCollection AddGoogleWebRisk(this IServiceCollection services, IConfiguration config)
    {
        string? googleJsonCredentials = config["GoogleServiceAccountJson"] ?? Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
        ArgumentNullException.ThrowIfNull(googleJsonCredentials, nameof(googleJsonCredentials));

        var builder = new WebRiskServiceClientBuilder
        {
            ChannelCredentials = GoogleCredential.FromJson(googleJsonCredentials).ToChannelCredentials()
        };

        services.AddSingleton<WebRiskServiceClient>(builder.Build());
        return services;
    }

    public static IServiceCollection AddAntivirusServices(this IServiceCollection services, IConfiguration config)
    {
        services
            .Configure<FileScanner.FileScannerOptions>(config.GetSection("FileScanner"))
            .PostConfigure<FileScanner.FileScannerOptions>(options =>
            {
                var envHost = Environment.GetEnvironmentVariable("ANTIVIRUS_SERVER_HOST");
                if (!string.IsNullOrEmpty(envHost)) options.AntivirusServerHost = envHost;

                var envPort = Environment.GetEnvironmentVariable("ANTIVIRUS_SERVER_PORT");
                if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var port)) options.AntivirusServerPort = port;

                var envSharedDirectory = Environment.GetEnvironmentVariable("SHARED_DIRECTORY");
                if (!string.IsNullOrEmpty(envSharedDirectory)) options.SharedDirectory = envSharedDirectory;
            });

        services
            .Configure<AntivirusScanQueue.AntivirusScanQueueOptions>(config.GetSection("AntivirusScanQueue"))
            .AddSingleton<IAntivirusScanQueue, AntivirusScanQueue>()
            .AddScoped<ScanResultCache>()
            .AddSingleton<FileScanner>()
            .AddSingleton<URLScanner>()
            .AddHostedService<AntivirusService>();

        return services;
    }

    public static IServiceCollection AddDiscordServices(this IServiceCollection services, IConfiguration config)
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        var interactionServiceConfig = new InteractionServiceConfig
        {
            UseCompiledLambda = true,
            AutoServiceScopes = true
        };

        services
            .AddScoped<BotDatabaseManager>()
            .AddSingleton(discordSocketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<DiscordLogHandler>()
            .AddSingleton<MalwareHandler>()
            .AddSingleton<MessageHandler>()
            .AddSingleton<InteractionService>(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<DiscordSocketClient>();
                return new InteractionService(client.Rest, interactionServiceConfig);
            });

        services
            .AddOptions<BotService.BotServiceOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                var token = config["DiscordToken"] ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");
                ArgumentNullException.ThrowIfNull(token, nameof(token));
                options.Token = token;
                options.TestGuildId = config.GetValue<ulong?>("Discord:TestGuildId");
                options.AdminCommandsGuildId = config.GetValue<ulong?>("Discord:AdminCommandsGuildId");
            });

        services.AddHostedService<BotService>();
        return services;
    }
}
