using System.ComponentModel.DataAnnotations;

namespace Template.Database.Entities;

public class ExampleEntity
{
    [Key]
    public ulong Id { get; set; }
}