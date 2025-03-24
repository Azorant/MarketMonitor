using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<CharacterEntity> Characters { get; set; }
    public DbSet<RetainerEntity> Retainers { get; set; }
    public DbSet<ListingEntity> Listings { get; set; }
    public DbSet<DatacenterEntity> Datacenters { get; set; }
    public DbSet<WorldEntity> Worlds { get; set; }
    public DbSet<ItemEntity> Items { get; set; }
    public DbSet<PurchaseEntity> Purchases { get; set; }
    public DbSet<SaleEntity> Sales { get; set; }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RetainerEntity>()
            .HasKey(r => new { r.Name, r.OwnerId });

        modelBuilder.Entity<CharacterEntity>()
            .HasKey(c => c.Id);
        modelBuilder.Entity<CharacterEntity>()
            .HasAlternateKey(c => c.Name);

        modelBuilder.Entity<ListingEntity>()
            .HasKey(l => l.Id);

        modelBuilder.Entity<ItemEntity>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<DatacenterEntity>()
            .HasKey(d => d.Name);

        modelBuilder.Entity<WorldEntity>()
            .HasKey(w => w.Id);

        modelBuilder.Entity<PurchaseEntity>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<SaleEntity>()
            .HasKey(l => l.Id);
    }
};