namespace MarketMonitor.Database.Entities;

public class DatacenterEntity
{
    public required string Name { get; set; }
    public required string Region { get; set; }
    public virtual ICollection<WorldEntity> Worlds { get; set; }
}