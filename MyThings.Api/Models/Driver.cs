namespace MyThings.Api.Models;

// Driver-only data. UserId is both the primary key and the FK to users (shared PK = 1:1).
public class Driver : TimestampedEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
