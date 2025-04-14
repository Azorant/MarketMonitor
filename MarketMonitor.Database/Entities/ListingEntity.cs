using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MarketMonitor.Database.Models;

namespace MarketMonitor.Database.Entities;

public class ListingEntity
{
    [MaxLength(64)]
    public required string Id { get; set; }
    public int ItemId { get; set; }
    public virtual ItemEntity Item { get; set; } = null!;
    public int PricePerUnit { get; set; }
    public int Quantity { get; set; }
    public double TaxRate { get; set; } = 0.95;
    [NotMapped]
    public double Total => Math.Floor((double)(Quantity * PricePerUnit)) * TaxRate;
    public DateTime UpdatedAt { get; set; }
    public ListingFlags Flags { get; set; } = ListingFlags.None;
    public bool IsHq { get; set; }
    [MaxLength(64)]
    public required string RetainerName { get; set; }
    public required ulong RetainerOwnerId { get; set; }
    public RetainerCity RetainerCity { get; set; } = RetainerCity.Unknown;
    public int WorldId { get; set; }
    public bool IsNotified { get; set; }
    public virtual RetainerEntity Retainer { get; set; } = null!;
    public virtual WorldEntity World { get; set; } = null!;
}