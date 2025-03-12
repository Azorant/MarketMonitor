using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketMonitor.Database.Entities;

public class RetainerEntity
{
    /// <summary>
    /// Retainer name
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }
    /// <summary>
    /// ID obtained via universalis when verifying ownership of retainer
    /// </summary>
    [MaxLength(64)]
    public required string Id { get; set; } = string.Empty;
    /// <summary>
    /// Owner ID
    /// </summary>
    public ulong OwnerId { get; set; }
    public virtual CharacterEntity Owner { get; set; } = null!;
    /// <summary>
    /// Is retainer verified
    /// </summary>
    public bool IsVerified { get; set; }
    public int? VerificationItem { get; set; }
    public int? VerificationPrice { get; set; }
    
    public virtual ICollection<ListingEntity> Listings { get; set; }
}