using System.Net;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Tower.Services.Discord;
public static class ChannelExtensions
{
    public static async Task TrySendMessageAsync(this ISocketMessageChannel channel,
                                                 string text,
                                                 Embed? embed = null,
                                                 ILogger? logger = null)
    {
        try
        {
            await channel.SendMessageAsync(text: text, embed: embed);
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            logger?.LogWarning("Permission denied when trying to send a message to channel {ChannelId}.", channel.Id);

            if (channel is not SocketGuildChannel guildChannel)
            {
                logger?.LogInformation("Channel with id {ChannelId} is not a guild channel. Returning...", channel.Id);
                return;
            }
            var owner = guildChannel.Guild.Owner;

            try
            {
                await owner.SendMessageAsync(
                    $"I attempted to send a message in the channel **#{guildChannel.Name}** ({guildChannel.Id}), " +
                    $"but I lack the necessary permissions. Here's the message:\n{text}",
                    embed: embed
                );
            }
            catch (Exception ownerEx)
            {
                logger?.LogError(ownerEx, "Failed to send a message to the server owner ({OwnerId}).", owner.Id);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while trying to send a message to channel {ChannelId}.", channel.Id);
        }
    }
}
