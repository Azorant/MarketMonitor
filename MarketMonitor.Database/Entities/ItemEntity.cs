using System.ComponentModel.DataAnnotations;

namespace MarketMonitor.Database.Entities;

public class ItemEntity
{
    public int Id { get; set; }
    [MaxLength(128)]
    public required string Name { get; set; }
    [MaxLength(256)]
    public required string IconPath {get; set;}
    public byte[]? IconData { get; set; }
}