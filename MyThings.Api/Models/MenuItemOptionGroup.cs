namespace MyThings.Api.Models;

// Join table: which option groups show on which menu item (many-to-many)
public class MenuItemOptionGroup
{
    public Guid Id { get; set; }

    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public Guid OptionGroupId { get; set; }
    public OptionGroup OptionGroup { get; set; } = null!;
}
