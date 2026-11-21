namespace MyThings.Api.Models;

// Join table: the options chosen for one cart line
public class CartItemOption
{
    public Guid Id { get; set; }

    public Guid CartItemId { get; set; }
    public CartItem CartItem { get; set; } = null!;

    public Guid MenuOptionId { get; set; }
    public MenuOption MenuOption { get; set; } = null!;
}
