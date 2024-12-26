using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Tower.Persistence;

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

                services
                    .AddDatabase(config)
                    .AddGoogleWebRisk(config)
                    .AddAntivirusServices(config)
                    .AddDiscordServices(config)
                    .AddQuartzAndJobs(config);
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

            var scheduler = await scope.ServiceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler();
            await scheduler.Start();
        }

        await host.RunAsync();
    }
}