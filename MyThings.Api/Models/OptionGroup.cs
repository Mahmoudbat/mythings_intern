namespace MyThings.Api.Models;

// Size, Extras, Sauce... belongs to the store so one group can be shared by many items
public class OptionGroup : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int MinSelect { get; set; }              // Size: 1, Extras: 0, Sauce: 0
    public int MaxSelect { get; set; }              // Size: 1, Extras: 3, Sauce: 2

    public ICollection<MenuOption> Options { get; set; } = new List<MenuOption>();
    public ICollection<MenuItemOptionGroup> MenuItemLinks { get; set; } = new List<MenuItemOptionGroup>();
}
