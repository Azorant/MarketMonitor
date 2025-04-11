using System.Text.Json;
using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Bot.Models.XIVAPI;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Services;

public class ApiService(DatabaseContext db)
{
    public async Task<T> Request<T>(string url)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MarketMonitor");
        HttpResponseMessage response = await client.GetAsync(new Uri(url));
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<T> RequestUniversalis<T>(string path)
    {
        return await Request<T>($"https://universalis.app/api/v2{path}");
    }

    // ReSharper disable once InconsistentNaming
    public async Task<T> RequestXIVAPI<T>(string path)
    {
        return await Request<T>($"https://v2.xivapi.com/api{path}");
    }

    public async Task UpdateCache(SocketInteractionContext context)
    {
        Log.Information("Updating DCs and Worlds");
        var statusEmbed = new EmbedBuilder()
            .WithTitle("Updating Database")
            .WithFields(new EmbedFieldBuilder()
                    .WithName("Datacenters and Worlds")
                    .WithValue("Updating"),
                new EmbedFieldBuilder()
                    .WithName("Items")
                    .WithValue("Queued"))
            .WithColor(Color.Blue);
        await context.Interaction.FollowupAsync(embed: statusEmbed.Build());
        var dcResponse = await RequestUniversalis<List<DatacenterResponse>>("/data-centers");
        var worldResponse = await RequestUniversalis<List<WorldResponse>>("/worlds");
        var existingWorlds = await db.Worlds.ToListAsync();
        var existingDatacenters = await db.Datacenters.ToListAsync();

        foreach (var dc in dcResponse)
        {
            var checkDc = existingDatacenters.FirstOrDefault(d => d.Name == dc.Name);
            if (checkDc == null)
            {
                checkDc = new DatacenterEntity
                {
                    Name = dc.Name,
                    Region = dc.Region,
                };
                await db.AddAsync(checkDc);
            }

            foreach (var worldId in dc.Worlds)
            {
                var world = worldResponse.FirstOrDefault(w => w.Id == worldId);
                if (world == null) continue;
                var checkWorld = existingWorlds.FirstOrDefault(w => w.Id == world.Id);
                if (checkWorld != null) continue;
                await db.AddAsync(new WorldEntity
                {
                    Id = world.Id,
                    Name = world.Name,
                    DatacenterName = dc.Name,
                });
            }
        }

        await db.SaveChangesAsync();
        Log.Information("DCs and Worlds updated");
        statusEmbed.Fields.First().Value = $"{dcResponse.Count:N0} Datacenters\n{worldResponse.Count:N0} Worlds";
        statusEmbed.Fields.Last().Value = "Updating\n0 items";
        await context.Interaction.ModifyOriginalResponseAsync(m => m.Embed = statusEmbed.Build());

        var itemResponses = new List<ItemData>();
        var fetchCount = 0;
        Log.Information("Updating items");
        while (true)
        {
            var response = await RequestXIVAPI<ItemResponse>($"/sheet/Item?limit=500&after={(itemResponses.Count == 0 ? 0 : itemResponses.Last().Id)}");
            if (response.Rows.Count == 0) break; // Fetched all items
            itemResponses.AddRange(response.Rows);
            fetchCount++;
            if (fetchCount % 10 == 0)
            {
                statusEmbed.Fields.Last().Value = $"Updating\n{itemResponses.Count:N0} items fetched";
                await context.Interaction.ModifyOriginalResponseAsync(m => m.Embed = statusEmbed.Build());
            }
        }

        var count = 0;

        foreach (var rawItem in itemResponses)
        {
            var exists = await db.Items.FirstOrDefaultAsync(i => i.Id == rawItem.Id);
            if (exists != null || string.IsNullOrEmpty(rawItem.Fields.Name)) continue;
            await db.AddAsync(new ItemEntity
            {
                Id = rawItem.Id,
                Icon = rawItem.Fields.Icon.Path,
                Name = rawItem.Fields.Name,
            });
            count++;
            // ReSharper disable once PossibleLossOfFraction
            if (count % Math.Floor((double)(itemResponses.Count / 10)) == 0)
            {
                statusEmbed.Fields.Last().Value = $"Updating\n{itemResponses.Count:N0} items fetched\n{count:N0} items added";
                await context.Interaction.ModifyOriginalResponseAsync(m => m.Embed = statusEmbed.Build());
            }
        }

        await db.SaveChangesAsync();
        Log.Information($"Updated {count:N0} items");
        statusEmbed.Title = "Database Updated";
        statusEmbed.Fields.Last().Value = $"{itemResponses.Count:N0} items fetched\n{count:N0} items added";
        statusEmbed.Color = Color.Green;
        await context.Interaction.ModifyOriginalResponseAsync(m => m.Embed = statusEmbed.Build());
    }

    public Task<MarketBoardDataResponse> FetchItem(int itemId, string region, int entries = 5, TimeSpan? entriesWithin = null) =>
        RequestUniversalis<MarketBoardDataResponse>(
            $"/{region}/{itemId}?entries={entries}{(entriesWithin == null ? string.Empty : $"&entriesWithin={entriesWithin.Value.TotalSeconds}")}");
}