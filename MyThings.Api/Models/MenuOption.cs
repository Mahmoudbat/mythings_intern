namespace MyThings.Api.Models;

// A choice inside a group: "Large", "Cheese", "Garlic"
public class MenuOption : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid OptionGroupId { get; set; }
    public OptionGroup OptionGroup { get; set; } = null!;

    public string Name { get; set; } = null!;
    public decimal ExtraPrice { get; set; }         // added to the item price

    public ICollection<CartItemOption> CartItemOptions { get; set; } = new List<CartItemOption>();
}
