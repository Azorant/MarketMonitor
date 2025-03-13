using Discord;
using Discord.WebSocket;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class MarketJob(DatabaseContext db, ApiService api, DiscordSocketClient client)
{
    public async Task Execute()
    {
        try
        {
            var listingGroups = await db.Listings.Include(l => l.World).Include(l => l.Item).Include(l => l.Retainer).ThenInclude(r => r.Owner).Where(l => !l.IsNotified)
                .GroupBy(l => l.ItemId).ToListAsync();

            var notifications = new Dictionary<ulong, Dictionary<string, List<string>>>();

            foreach (var listings in listingGroups)
            {
                var itemId = listings.Key;
                var dcGroups = listings.GroupBy(l => l.World.DatacenterName);
                foreach (var dcGroup in dcGroups)
                {
                    var market = await api.FetchItem(itemId, dcGroup.Key);

                    var retainerGroups = dcGroup.GroupBy(l => new { l.RetainerName, l.RetainerOwnerId });
                    foreach (var retainerGroup in retainerGroups)
                    {
                        var retainer = retainerGroup.First();
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

                        retainerDict.Add($"[{retainer.Item.Name}](https://universalis.app/market/{retainer.ItemId})");
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
                        .WithDescription("The following items have been undercut.")
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
        catch (Exception e)
        {
            Log.Error(e, "Error while checking market board");
        }
    }
}