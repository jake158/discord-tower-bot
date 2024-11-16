using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Tower.Services.Discord;
public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly BotDatabaseManager _dbManager;

    public CommandModule(BotDatabaseManager databaseManager)
    {
        _dbManager = databaseManager;
    }

    [SlashCommand("help", "Learn how to use Tower")]
    public async Task HelpCommandAsync()
    {
        await RespondAsync("Pong");
    }

    [CommandContextType(InteractionContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("setalertchannel", "Set a moderator-only channel to send bot alerts to")]
    public async Task SetAlertChannelCommandAsync([ChannelTypes(ChannelType.Text)] IChannel channel)
    {
        var guild = (Context.Channel as SocketGuildChannel)?.Guild;
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));

        var guildSettings = await _dbManager.GetGuildSettingsAsync(guild);
        guildSettings.AlertChannel = channel.Id;

        await _dbManager.SaveGuildSettingsAsync(guildSettings);

        await RespondAsync($"Alert channel successfully set to <#{channel.Id}>", ephemeral: true);
    }
}