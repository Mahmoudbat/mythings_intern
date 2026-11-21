namespace MyThings.Api.Models;

// One order line with copied values (Option B: no FK to the menu)
public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid? MenuItemId { get; set; }           // plain copy, NO FK and no navigation (Option B)
    public string Name { get; set; } = null!;
    public decimal UnitPrice { get; set; }          // item price at checkout, without options
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public decimal LineTotal { get; set; }          // (unit price + options) * quantity

    public ICollection<OrderItemOption> Options { get; set; } = new List<OrderItemOption>();
}
