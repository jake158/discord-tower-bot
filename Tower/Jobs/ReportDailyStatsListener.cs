using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Listener;

namespace Tower.Jobs;
public class ReportDailyStatsListener(IServiceScopeFactory scopeFactory) : JobListenerSupport
{
    public override string Name => "ReportDailyStatsListener";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;


    public async override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        if (jobException == null)
        {
            using var scope = _scopeFactory.CreateScope();
            var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();

            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            await scheduler.TriggerJob(DailyRateResetJob.Key, cancellationToken);
        }
    }
}