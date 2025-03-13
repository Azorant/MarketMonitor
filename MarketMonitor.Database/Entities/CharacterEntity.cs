using System.ComponentModel.DataAnnotations;

namespace MarketMonitor.Database.Entities;

public class CharacterEntity
{
    /// <summary>
    /// Discord ID
    /// </summary>
    public ulong Id { get; set; }
    public string LodestoneId { get; set; }
    /// <summary>
    /// Character name
    /// </summary>
    [MaxLength(64)]
    public required string Name { get; set; }
    /// <summary>
    /// Is character verified
    /// </summary>
    public bool IsVerified { get; set; }
    public Guid? VerificationValue { get; set; } = Guid.NewGuid();
    public string DatacenterName { get; set; }
    public int? NotificationRegionId { get; set; }
    
    public virtual DatacenterEntity Datacenter { get; set; }
    public virtual WorldEntity? NotificationRegion { get; set; }
    public virtual ICollection<RetainerEntity> Retainers { get; set; }
}