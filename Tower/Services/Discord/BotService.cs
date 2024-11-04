using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tower.Services.Discord;
internal sealed class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordLogHandler _discordLogHandler;
    private readonly MessageHandler _messageHandler;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly BotServiceOptions _options;
    private readonly Assembly _currentAssembly;
    private readonly ILogger<BotService> _logger;

    public BotService(
        DiscordSocketClient client,
        DiscordLogHandler discordLogHandler,
        MessageHandler messageHandler,
        InteractionService interactionService,
        IServiceProvider services,
        IOptions<BotServiceOptions> options,
        ILogger<BotService> logger)
    {
        _client = client;
        _discordLogHandler = discordLogHandler;
        _messageHandler = messageHandler;
        _interactionService = interactionService;
        _services = services;
        _options = options.Value;
        _currentAssembly = Assembly.GetExecutingAssembly();
        _logger = logger;
    }

    public class BotServiceOptions
    {
        [Required]
        public string Token { get; set; } = "";

        public ulong? TestGuildID { get; set; } = null;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += _discordLogHandler.LogAsync;
        _client.MessageReceived += _messageHandler.HandleMessageAsync;
        _interactionService.Log += _discordLogHandler.LogAsync;

        _client.InteractionCreated += async (x) =>
        {
            var ctx = new SocketInteractionContext(_client, x);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        };

        _client.Ready += async () => 
        {
            _logger.LogInformation($"WebSocket connection established. Latency: {_client.Latency} ms");

            if (_options.TestGuildID != null)
            {
                _logger.LogInformation($"Registering commands into guild with ID: {_options.TestGuildID}...");
                await _interactionService.RegisterCommandsToGuildAsync((ulong)_options.TestGuildID);
            }
            else
            {
                _logger.LogInformation("Registering commands globally...");
                await _interactionService.RegisterCommandsGloballyAsync();
            }
        };


        _logger.LogInformation("Starting Tower...");

        await _interactionService.AddModulesAsync(_currentAssembly, _services);

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
