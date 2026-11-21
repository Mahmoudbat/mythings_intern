namespace MyThings.Api.Models;

public class OrderItemOption
{
    public Guid Id { get; set; }

    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public string GroupName { get; set; } = null!;  // "Sauce"
    public string OptionName { get; set; } = null!; // "Ketchup"
    public decimal ExtraPrice { get; set; }
}
