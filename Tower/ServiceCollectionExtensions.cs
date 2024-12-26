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
using Quartz;
using Tower.Jobs;
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
            var dbSettings = config.GetSection("DatabaseSettings");

            var password = dbSettings["Password"] ?? Environment.GetEnvironmentVariable("DB_PASSWORD");
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var port = dbSettings["Port"] ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";

            var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                Password = password,
                DataSource = $"{host},{port}"
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
                var discordConfig = config.GetRequiredSection("Discord");
                var token = discordConfig["Token"] ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");
                ArgumentNullException.ThrowIfNull(token, nameof(token));

                options.Token = token;
                options.TestGuildId = discordConfig.GetValue<ulong?>("Commands:TestGuildId");
                options.AdminCommandsGuildId = discordConfig.GetValue<ulong?>("Commands:AdminCommandsGuildId");
            });

        services.AddHostedService<BotService>();
        return services;
    }

    public static IServiceCollection AddQuartzAndJobs(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddQuartz(q =>
            {
                // When sharding:
                // q.SchedulerId = "Scheduler-Core";

                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 2;
                });

                q.AddJob<ReportDailyStatsJob>(ReportDailyStatsJob.Key, j => j
                    .WithDescription("Job to report daily usage stats"));

                q.AddTrigger(t => t
                    .WithIdentity("Daily midnight UTC trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 0))
                    .ForJob(ReportDailyStatsJob.Key)
                    .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddMinutes(2))));
            });

        services
            .Configure<ReportDailyStatsJob.ReportDailyStatsJobOptions>(config.GetSection("Discord:Jobs"))
            .AddTransient<ReportDailyStatsJob>();

        return services;
    }
}
