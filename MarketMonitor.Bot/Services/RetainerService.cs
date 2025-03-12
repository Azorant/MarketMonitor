using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Services;

public class RetainerService
{
    private readonly DatabaseContext db;
    public List<string> Retainers { get; set; } = new();

    public RetainerService(DatabaseContext db)
    {
        this.db = db;
        _ = LoadRetainers();
    }

    public async Task LoadRetainers()
    {
        Retainers = await db.Retainers.Select(r => r.Name).ToListAsync();
    }
}