using System.ComponentModel.DataAnnotations.Schema;

namespace MarketMonitor.Database.Entities;

public class PurchaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public int PricePerUnit { get; set; }
    public bool IsHq { get; set; }
    public DateTime PurchasedAt { get; set; }
    public ulong CharacterId { get; set; }
    public int WorldId { get; set; }
    
    public virtual ItemEntity Item { get; set; }
    public virtual WorldEntity World { get; set; }
    public virtual CharacterEntity Character { get; set; }
}