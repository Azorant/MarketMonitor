using System.Text.Json;
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
        return JsonSerializer.Deserialize<T>(str, new JsonSerializerOptions {PropertyNameCaseInsensitive = true})!;
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

    public async Task UpdateCache()
    {
        Log.Information("Updating DCs and Worlds");
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

        var itemResponses = new List<ItemData>();

        Log.Information("Updating items");
        while (true)
        {
            var response = await RequestXIVAPI<ItemResponse>($"/sheet/Item?limit=500&after={(itemResponses.Count == 0 ? 0 : itemResponses.Last().Id)}");
            if (response.Rows.Count == 0) break; // Fetched all items
            itemResponses.AddRange(response.Rows);
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
        }

        await db.SaveChangesAsync();
        Log.Information($"Updated {count:N0} items");
    }
}