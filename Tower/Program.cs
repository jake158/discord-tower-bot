﻿using Discord;
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

            services.AddDbContext<TowerDbContext>(options =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");
                var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    Password = config["ConnectionStrings:SqlServerPassword"]
                };
                var connection = conStrBuilder.ConnectionString;

                options.UseSqlServer(connection);
            });


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
