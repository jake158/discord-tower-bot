using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Discord;
internal sealed class LoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(DiscordSocketClient client, ILogger<LoggingService> logger)
    {
        _logger = logger;
        client.Log += LogAsync;
    }

    private Task LogAsync(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };
        _logger.Log(logLevel, message.Exception, message.Message);

        return Task.CompletedTask;
    }
}
