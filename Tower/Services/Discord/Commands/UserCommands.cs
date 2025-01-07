using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Tower.Services.Discord.Commands;
public class UserCommands(BotDatabaseManager databaseManager) : InteractionModuleBase<SocketInteractionContext>
{
    private readonly BotDatabaseManager _dbManager = databaseManager;

    [SlashCommand("help", "Learn how to use Tower")]
    public async Task HelpCommandAsync()
    {
        await RespondAsync("Pong");
    }

    [CommandContextType(InteractionContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("alertchannel", "Set a channel to send bot alerts to")]
    public async Task AlertChannelCommandAsync([ChannelTypes(ChannelType.Text)] IChannel channel)
    {
        await DeferAsync();

        var guild = (Context.Channel as SocketGuildChannel)?.Guild;
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        var guildSettings = await _dbManager.GetGuildSettingsAsync(guild);
        guildSettings.AlertChannel = channel.Id;

        await _dbManager.SaveGuildSettingsAsync(guildSettings);

        await FollowupAsync($"Alert channel successfully set to <#{channel.Id}>", ephemeral: true);
    }

    [CommandContextType(InteractionContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("togglescans", "Enable or disable scans for the entire guild.")]
    public async Task ToggleScansCommandAsync(bool enabled)
    {
        await DeferAsync();

        var guild = (Context.Channel as SocketGuildChannel)?.Guild;
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        var guildSettings = await _dbManager.GetGuildSettingsAsync(guild);
        guildSettings.IsScanEnabled = enabled;

        await _dbManager.SaveGuildSettingsAsync(guildSettings);

        await FollowupAsync($"Scans {(enabled ? "enabled" : "disabled")} successfully.", ephemeral: true);
    }
}