using Discord.Interactions;

namespace Tower.Services.Discord;
public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Learn how to use Tower")]
    public async Task HelpCommand()
    {
        await RespondAsync("Pong");
    }

}