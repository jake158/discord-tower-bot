using Discord;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Discord;
internal sealed class DiscordLogHandler
{
    private readonly ILogger<DiscordLogHandler> _logger;

    public DiscordLogHandler(ILogger<DiscordLogHandler> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(LogMessage message)
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
        _logger.Log(logLevel, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }
}
