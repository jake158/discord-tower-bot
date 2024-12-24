using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tower.Services.Discord.Commands;

namespace Tower.Services.Discord;
internal sealed class BotService(
    DiscordSocketClient client,
    DiscordLogHandler discordLogHandler,
    MessageHandler messageHandler,
    InteractionService interactionService,
    IServiceProvider services,
    IOptions<BotService.BotServiceOptions> options,
    ILogger<BotService> logger) : IHostedService
{
    private readonly DiscordSocketClient _client = client;
    private readonly DiscordLogHandler _discordLogHandler = discordLogHandler;
    private readonly MessageHandler _messageHandler = messageHandler;
    private readonly InteractionService _interactionService = interactionService;
    private readonly IServiceProvider _services = services;
    private readonly BotServiceOptions _options = options.Value;
    private readonly Assembly _currentAssembly = Assembly.GetExecutingAssembly();
    private readonly ILogger<BotService> _logger = logger;

    public class BotServiceOptions
    {
        [Required]
        public string Token { get; set; } = "";

        public ulong? TestGuildId { get; set; } = null;
        public ulong? AdminCommandsGuildId { get; set; } = null;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += _discordLogHandler.LogAsync;
        _client.MessageReceived += _messageHandler.HandleMessageAsync;
        _interactionService.Log += _discordLogHandler.LogAsync;

        _client.SlashCommandExecuted += async (interaction) =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        };
        _client.ButtonExecuted += async (interaction) =>
        {
            var ctx = new SocketInteractionContext<SocketMessageComponent>(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        };

        _client.Ready += async () =>
        {
            _logger.LogInformation($"WebSocket connection established. Latency: {_client.Latency} ms");

            await _interactionService.AddModulesAsync(_currentAssembly, _services);

            if (_options.TestGuildId.HasValue)
            {
                _logger.LogInformation($"Registering commands into guild with ID: {_options.TestGuildId.Value}...");
                await _interactionService.RegisterCommandsToGuildAsync(_options.TestGuildId.Value);
            }
            else
            {
                _logger.LogInformation("Registering commands globally...");
                await _interactionService.RegisterCommandsGloballyAsync();
            }

            if (_options.AdminCommandsGuildId.HasValue)
            {
                _logger.LogInformation($"Registering admin commands into guild with ID: {_options.AdminCommandsGuildId.Value}...");
                var ownerCommandsInfo = _interactionService.GetModuleInfo<AdminCommands>();
                await _interactionService.AddModulesToGuildAsync(_options.AdminCommandsGuildId.Value, deleteMissing: false, ownerCommandsInfo);
            }
            else
            {
                _logger.LogInformation("No admin commands guild specified. Skipping registration...");
            }
        };

        _logger.LogInformation("Starting Tower...");


        await _client.LoginAsync(TokenType.Bot, _options.Token);
        await _client.StartAsync();

        _logger.LogInformation("Tower has started");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cancellation requested");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Tower...");

        await _client.StopAsync();

        _logger.LogInformation("Tower has stopped");
    }
}
