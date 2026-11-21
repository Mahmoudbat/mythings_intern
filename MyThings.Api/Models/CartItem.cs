namespace MyThings.Api.Models;

public class CartItem : TimestampedEntity
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; }
    public string? Note { get; set; }               // "no onions"

    public ICollection<CartItemOption> Options { get; set; } = new List<CartItemOption>();
}
