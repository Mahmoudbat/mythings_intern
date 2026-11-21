namespace MyThings.Api.Models;

public class Store : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = null!;
    public StoreCategory Category { get; set; }
    public bool IsPaused { get; set; }              // store paused new orders (too busy)
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
    public decimal DeliveryFee { get; set; }
    public string City { get; set; } = null!;
    public string? StreetName { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();
    public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
