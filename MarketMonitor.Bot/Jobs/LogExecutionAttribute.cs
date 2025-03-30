﻿using Hangfire.Common;
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
            nameof(CacheJob.PopulateCharacterCache) => "character_cache",
            nameof(CacheJob.PopulateListingCache) => "listing_cache",
            nameof(HandlePacketJob.HandleListingAdd) => "listing/add",
            nameof(HandlePacketJob.HandleListingRemove) => "listing/remove",
            nameof(HandlePacketJob.HandleSaleAdd) => "sale/add",
            nameof(HealthJob.CheckHealth) => "health",
            nameof(MarketJob.CheckMarket) => "market",
            nameof(StatusJob.SetStatus) => "status",
            nameof(HandleSaleJob.Handle) => "sales",
            _ => context.BackgroundJob.Job.Method.Name
        };

        stats.JobExecuted.WithLabels(name).Observe(state.PerformanceDuration);
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    { }
}