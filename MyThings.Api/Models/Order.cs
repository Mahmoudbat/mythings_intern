namespace MyThings.Api.Models;

// "order" is a reserved word in SQL, but EF quotes names for us. The table is "Orders".
public class Order : TimestampedEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? StoreId { get; set; }              // NULL if the store was deleted later
    public Store? Store { get; set; }

    public Guid? DriverId { get; set; }             // NULL until a driver takes it
    public Driver? Driver { get; set; }

    // snapshots, copied at "Place order"
    public string StoreName { get; set; } = null!;
    public string AddressName { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Area { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string BuildingNumber { get; set; } = null!;
    public int Floor { get; set; }
    public string? AddressNote { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }              // lines + delivery fee

    public string? Note { get; set; }               // "Please ring the bell twice."
    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    // when each step happened (CreatedAt = placed)
    public DateTime? AcceptedAt { get; set; }
    public DateTime? OnTheWayAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
