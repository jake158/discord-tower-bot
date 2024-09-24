using System.ComponentModel.DataAnnotations;
using Discord;
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
    private readonly ILogger<BotService> _logger;
    private readonly string _token;

    public BotService(
        IOptions<Settings> options,
        DiscordSocketClient client,
        DiscordLogHandler discordLogHandler,
        MessageHandler messageHandler,
        ILogger<BotService> logger)
    {
        _token = options.Value.Token;
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

        await _client.LoginAsync(TokenType.Bot, _token);
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

    public class Settings
    {
        [Required]
        public string Token { get; set; } = "";
    }
}
