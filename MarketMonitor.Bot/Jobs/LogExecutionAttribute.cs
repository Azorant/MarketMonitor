using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using MarketMonitor.Bot.Services;

namespace MarketMonitor.Bot.Jobs;

public class LogExecutionAttribute(PrometheusService stats) : JobFilterAttribute, IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not SucceededState state) return;

        var name = context.BackgroundJob.Job.Method.Name switch
        {
            nameof(CacheJob.PopulateCharacterCache) => "character/cache",
            nameof(CacheJob.PopulateListingCache) => "listing/cache",
            nameof(PacketJob.HandleListingAdd) => "listing/add",
            nameof(PacketJob.HandleListingRemove) => "listing/remove",
            nameof(PacketJob.HandlePurchaseAdd) => "purchase/add",
            nameof(PacketJob.HandleSaleAdd) => "sale/add",
            nameof(HealthJob.CheckHealth) => "health",
            nameof(MarketJob.UndercutCheck) => "market/undercut",
            nameof(MarketJob.DailyListingCheck) => "market/daily",
            nameof(StatusJob.SetStatus) => "status",
            _ => context.BackgroundJob.Job.Method.Name
        };

        stats.JobExecuted.WithLabels(name).Observe(state.PerformanceDuration);
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    { }
}