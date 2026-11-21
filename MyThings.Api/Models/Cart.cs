namespace MyThings.Api.Models;

// One cart per user, one store per cart
public class Cart : TimestampedEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
