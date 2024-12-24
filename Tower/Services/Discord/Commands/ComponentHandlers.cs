using Discord.Interactions;
using Discord.WebSocket;
using Tower.Persistence;

namespace Tower.Services.Discord.Commands;
public class ComponentHandlers : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    private readonly TowerDbContext _db;

    public ComponentHandlers(TowerDbContext db)
    {
        _db = db;
    }

    [RequireTeam]
    [ComponentInteraction("statsview:*,*")]
    public async Task HandleDumpStatsPagination(string action, string pageStr)
    {
        int page = int.Parse(pageStr);
        page = action == "next" ? page + 1 : page - 1;

        var (embed, components) = await AdminCommandViews.GetStatsViewAsync(_db, page);

        await Context.Interaction.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }

    [RequireTeam]
    [ComponentInteraction("alloffensesview:*,*")]
    public async Task HandleGetAllOffensesPagination(string action, string pageStr)
    {
        int page = int.Parse(pageStr);
        page = action == "next" ? page + 1 : page - 1;

        var (embed, components) = await AdminCommandViews.GetOffensesViewAsync(_db, page);

        await Context.Interaction.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }

    [RequireTeam]
    [ComponentInteraction("useroffensesview:*,*,*")]
    public async Task HandleGetUserOffensesPagination(string userIdStr, string action, string pageStr)
    {
        ulong userId = ulong.Parse(userIdStr);
        int page = int.Parse(pageStr);
        page = action == "next" ? page + 1 : page - 1;

        var (embed, components) = await AdminCommandViews.GetUserOffensesViewAsync(_db, userId, page);

        await Context.Interaction.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }
}
