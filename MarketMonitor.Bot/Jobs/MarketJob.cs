using Discord;
using Discord.WebSocket;
using Hangfire;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class MarketJob(DatabaseContext db, ApiService api, DiscordSocketClient client, CacheService cacheService, PacketJob job)
{
    [TypeFilter(typeof(LogExecutionAttribute)), DisableConcurrentExecution("listings", 120)]
    public async Task UndercutCheck()
    {
        cacheService.Emotes.TryGetValue("gil", out var gilEmote);

        var listingGroups = await db.Listings
            .Include(l => l.World)
            .Include(l => l.Item)
            .Include(l => l.Retainer)
            .ThenInclude(r => r.Owner)
            .Where(l => l.Flags == ListingFlags.None && l.Retainer.Owner.UndercutNotification)
            .GroupBy(l => l.ItemId).ToListAsync();

        var notifications = new Dictionary<ulong, Dictionary<string, List<string>>>();

        foreach (var listings in listingGroups)
        {
            var itemId = listings.Key;
            var dcGroups = listings.GroupBy(l => l.World.DatacenterName);
            foreach (var dcGroup in dcGroups)
            {
                var market = await api.FetchItem(itemId, dcGroup.Key, 50);

                // Check for undercuts
                var retainerGroups = dcGroup.Where(l => !l.Flags.HasFlag(ListingFlags.Removed) && !l.IsNotified).GroupBy(l => new { l.RetainerName, l.RetainerOwnerId });
                foreach (var retainerGroup in retainerGroups)
                {
                    var retainer = retainerGroup.OrderBy(r => r.PricePerUnit).First();
                    var region = retainer.Retainer.Owner.NotificationRegionId;

                    var cheapestNq = market.Listings.Where(l => !l.Hq && (region == null || l.WorldId == region)).DefaultIfEmpty().Min(l => l?.PricePerUnit);
                    var cheapestHq = market.Listings.Where(l => l.Hq && (region == null || l.WorldId == region)).DefaultIfEmpty().Min(l => l?.PricePerUnit);

                    if (retainer.IsHq && cheapestHq >= retainer.PricePerUnit || !retainer.IsHq && cheapestNq >= retainer.PricePerUnit)
                        continue;
                    foreach (var listing in retainerGroup)
                    {
                        listing.IsNotified = true;
                        db.Update(listing);
                    }

                    if (!notifications.TryGetValue(retainerGroup.Key.RetainerOwnerId, out var userDict))
                    {
                        userDict = new Dictionary<string, List<string>>();
                        notifications.Add(retainerGroup.Key.RetainerOwnerId, userDict);
                    }

                    if (!userDict.TryGetValue(retainerGroup.Key.RetainerName, out var retainerDict))
                    {
                        retainerDict = new List<string>();
                        userDict.Add(retainerGroup.Key.RetainerName, retainerDict);
                    }

                    var cutAmount = retainer.PricePerUnit - (retainer.IsHq ? cheapestHq : cheapestNq);
                    var cutText = string.Empty;
                    if (cutAmount != null)
                    {
                        cutText = $"{(gilEmote == null ? "" : $" {gilEmote}")} {cutAmount:N0} ({(double)cutAmount / retainer.PricePerUnit * 100:N1}%)";
                    }

                    retainerDict.Add($"[{retainer.Item.Name}](https://universalis.app/market/{retainer.ItemId}){cutText}");
                }
            }
        }

        await db.SaveChangesAsync();

        foreach (var user in notifications)
        {
            try
            {
                var socketUser = await client.GetUserAsync(user.Key);
                if (socketUser == null) continue;
                var channel = await socketUser.CreateDMChannelAsync();
                if (channel == null) continue;
                await channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("Undercut Alert")
                    .WithDescription("Below are the items that have been undercut and the amounts they were cut by.")
                    .WithColor(Color.Red)
                    .WithFields(user.Value.Select(r => new EmbedFieldBuilder().WithName(r.Key).WithValue(string.Join("\n", r.Value))))
                    .WithCurrentTimestamp()
                    .Build());
            }
            catch (Exception e)
            {
                Log.Error(e, "Error sending undercut notification");
            }
        }
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task ListingCheck()
    {
        var listingGroups = await db.Listings
            .Include(l => l.World)
            .Include(l => l.Item)
            .Include(l => l.Retainer)
            .ThenInclude(r => r.Owner)
            .Where(l => l.Flags == ListingFlags.None)
            .GroupBy(l => l.ItemId).ToListAsync();

        foreach (var listings in listingGroups)
        {
            var itemId = listings.Key;
            var dcGroups = listings.GroupBy(l => l.World.DatacenterName);
            foreach (var dcGroup in dcGroups)
            {
                var market = await api.FetchItem(itemId, dcGroup.Key, 50);

                // Check for stale listings 
                foreach (var listing in dcGroup)
                {
                    var matchingListing = market.Listings.FirstOrDefault(l => l.ListingId == listing.Id);
                    // Has matching listing meaning still on market
                    if (matchingListing != null)
                    {
                        listing.UpdatedAt = matchingListing.LastReviewTime.ConvertTimestamp();
                        if (listing.Quantity != matchingListing.Quantity || listing.PricePerUnit != matchingListing.PricePerUnit)
                        {
                            listing.Quantity = matchingListing.Quantity;
                            listing.PricePerUnit = matchingListing.PricePerUnit;
                            listing.IsNotified = false;
                        }

                        db.Update(listing);
                    }
                    else
                    {
                        SaleData? matchingSale = null;
                        while (matchingSale == null || market.RecentHistory.Count > 0)
                        {
                            var match = market.RecentHistory.OrderByDescending(h => h.Timestamp).FirstOrDefault(h =>
                                h.PricePerUnit == listing.PricePerUnit &&
                                h.Quantity == listing.Quantity &&
                                h.WorldId == listing.WorldId &&
                                h.Hq == listing.IsHq
                            );
                            if (match == null) break;

                            var alreadyHandled = await cacheService.MarketSaleCache(itemId, dcGroup.Key, match);
                            if (!alreadyHandled) matchingSale = match;

                            market.RecentHistory.Remove(match);
                        }

                        // Mark listing as removed
                        listing.Flags = listing.Flags.AddFlag(ListingFlags.Removed);
                        listing.UpdatedAt = DateTime.UtcNow;
                        db.Update(listing);

                        if (matchingSale == null) continue;

                        // Enqueue sale 
                        BackgroundJob.Schedule(() => job.HandleSaleAdd(itemId, listing.WorldId, matchingSale), TimeSpan.FromMinutes(5));
                    }
                }
            }
        }
        await db.SaveChangesAsync();
    }
}