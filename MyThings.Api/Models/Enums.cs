namespace MyThings.Api.Models;

// These replace the CHECK (x IN (...)) lists from the SQL sketch.
// They're stored as text in the database ("Male", "OnTheWay"...), see the configurations.

public enum Gender
{
    Male,
    Female
}

public enum StoreCategory
{
    Restaurant,
    Groceries,
    Cafe
}

public enum PaymentMethod
{
    Cash,
    Card
}

public enum OrderStatus
{
    Placed,
    Accepted,
    OnTheWay,
    Delivered,
    Declined,
    Cancelled
}
