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
    [MaxLength(64)]
    public required string RetainerId { get; set; }
    public virtual RetainerEntity Retainer { get; set; } = null!;
}