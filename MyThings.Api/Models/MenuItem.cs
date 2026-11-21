namespace MyThings.Api.Models;

public class MenuItem : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsSoldOut { get; set; }             // still visible, but can't be added to cart

    public ICollection<MenuItemOptionGroup> OptionGroupLinks { get; set; } = new List<MenuItemOptionGroup>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
