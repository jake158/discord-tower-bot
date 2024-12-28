using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Tower.Persistence;

namespace Tower.Jobs;

[DisallowConcurrentExecution]
public class DailyRateResetJob(ILogger<DailyRateResetJob> logger, TowerDbContext db) : IJob
{
    public static readonly JobKey Key = new("daily-rate-reset-job", "maintenance");
    public static readonly int RefireCount = 5;

    private readonly ILogger<DailyRateResetJob> _logger = logger;
    private readonly TowerDbContext _db = db;

    public async Task Execute(IJobExecutionContext context)
    {
        if (context.RefireCount > RefireCount)
        {
            _logger.LogError($"Job execution failed after attempting {RefireCount} refires.");
            return;
        }

        _logger.LogInformation("Beginning rate reset job...");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Resetting GuildStats scans today...");
            await _db.GuildStats.ExecuteUpdateAsync(g => g.SetProperty(gs => gs.ScansToday, 0));

            _logger.LogInformation("Resetting UserStats scans today...");
            await _db.UserStats.ExecuteUpdateAsync(g => g.SetProperty(gs => gs.ScansToday, 0));

            await transaction.CommitAsync();

            _logger.LogInformation("Daily rate reset job completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute daily rate reset job");

            await transaction.RollbackAsync();

            throw new JobExecutionException(msg: "DailyRateResetJob failed", refireImmediately: true, cause: ex);
        }
    }
}
