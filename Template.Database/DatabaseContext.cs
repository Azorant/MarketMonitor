using Microsoft.EntityFrameworkCore;
using Serilog;
using Template.Database.Entities;

namespace Template.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<ExampleEntity> Examples { get; set; }

    public async Task ApplyMigrations()
    {
        var pending = (await Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Any())
        {
            Log.Information($"Applying {pending.Count} migrations: {string.Join(", ", pending)}");
            await Database.MigrateAsync();
            Log.Information("Migrations applied");
        }
        else
        {
            Log.Information("No migrations to apply.");
        }
    }
};