namespace MyThings.Api.Models;

public class User : TimestampedEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string PhoneNum { get; set; } = null!;
    public Gender Gender { get; set; }

    public Guid? LastUsedAddressId { get; set; }
    public Address? LastUsedAddress { get; set; }

    // Navigations: the "other side" of foreign keys that point at users
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Store> OwnedStores { get; set; } = new List<Store>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Cart? Cart { get; set; }       // 1 : 0..1
    public Driver? Driver { get; set; }   // 1 : 0..1
}
