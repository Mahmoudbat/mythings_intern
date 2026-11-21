namespace MyThings.Api.Models;

public class Address : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = null!;       // "Home", "Work"
    public string City { get; set; } = null!;
    public string Area { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string BuildingNumber { get; set; } = null!;
    public int Floor { get; set; }
    public string? Note { get; set; }               // note for the driver (optional)
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
