namespace MyThings.Api.Models;

public class Section : TimestampedEntity
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }              // display order in the app

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
