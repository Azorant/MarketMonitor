using System.ComponentModel.DataAnnotations;

namespace MarketMonitor.Database.Entities;

public class ListingEntity
{
    [MaxLength(64)]
    public required string Id { get; set; }
    public int ItemId { get; set; }
    public virtual ItemEntity Item { get; set; } = null!;
    public int PricePerUnit { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsRemoved { get; set; }
    public bool IsHq { get; set; }
    [MaxLength(64)]
    public required string RetainerName { get; set; }
    public required ulong RetainerOwnerId { get; set; }
    public int WorldId { get; set; }
    public bool IsNotified { get; set; }
    public virtual RetainerEntity Retainer { get; set; } = null!;
    public virtual WorldEntity World { get; set; } = null!;
}