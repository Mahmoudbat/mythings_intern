namespace MyThings.Api.Models;

// Base class for tables that have created_at / updated_at.
// MyThingsDbContext.SaveChanges fills these in automatically (UTC).
public abstract class TimestampedEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
