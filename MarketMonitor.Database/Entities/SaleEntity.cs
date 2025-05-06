using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketMonitor.Database.Entities;

public class SaleEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string BuyerName { get; set; }
    public required string ListingId { get; set; }
    public required string ListingRetainerName { get; set; }
    public virtual ListingEntity Listing { get; set; }
    public DateTime BoughtAt { get; set; }
}