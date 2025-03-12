namespace MarketMonitor.Database.Entities;

public class WorldEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string DatacenterName { get; set; }
    public virtual DatacenterEntity Datacenter { get; set; }
}