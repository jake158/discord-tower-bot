using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tower.Services.Configuration;

namespace Tower.Services.Discord;
internal sealed class BotService : IHostedService
{
    private readonly Settings _settings;
    private readonly DiscordSocketClient _client;
    private readonly DiscordLogHandler _discordLogHandler;
    private readonly MessageHandler _messageHandler;
    private readonly ILogger<BotService> _logger;

    public BotService(
        Settings settings,
        DiscordSocketClient client,
        DiscordLogHandler discordLogHandler,
        MessageHandler messageHandler,
        ILogger<BotService> logger)
    {
        _settings = settings;
        _client = client;
        _discordLogHandler = discordLogHandler;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += _discordLogHandler.LogAsync;
        _client.MessageReceived += _messageHandler.HandleMessageAsync;

        _logger.LogInformation("Starting Tower...");

        await _client.LoginAsync(TokenType.Bot, _settings.Token);
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
