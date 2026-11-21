using Microsoft.EntityFrameworkCore;
using MyThings.Api.Models;

namespace MyThings.Api.Data;

// The DbContext is EF Core's connection to the database (like db.js in Express).
// Each DbSet is one table you can query: db.Stores.Where(s => s.City == "Amman")
public class MyThingsDbContext(DbContextOptions<MyThingsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<OptionGroup> OptionGroups => Set<OptionGroup>();
    public DbSet<MenuOption> MenuOptions => Set<MenuOption>();
    public DbSet<MenuItemOptionGroup> MenuItemOptionGroups => Set<MenuItemOptionGroup>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartItemOption> CartItemOptions => Set<CartItemOption>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Loads every class in Data/Configurations (one per table)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyThingsDbContext).Assembly);
    }

    // SQL Server has no "ON UPDATE", so CreatedAt / UpdatedAt are set here, right before saving.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;   // stored in UTC, shown in Asia/Amman by the app

        foreach (var entry in ChangeTracker.Entries<TimestampedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
