using PolesTaxi.Shared.Types;

namespace PolesTaxi.AnalyticService.Entity.Service;

public sealed class UserRegisteredFact
{
    public required string UserId { get; init; }
    public Role Role { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class OrderCompletedFact
{
    public required string OrderId { get; init; }
    public required string UserId { get; init; }
    public string? DriverId { get; init; }
    public TaxiType TaxiType { get; init; }
    public decimal Price { get; init; }
    public long DistanceMeters { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class TripRatedFact
{
    public required string OrderId { get; init; }
    public required string FromRole { get; init; }
    public int Score { get; init; }
    public DateTime EventTime { get; init; }
}

public sealed class OrderStats
{
    public long TotalOrders { get; init; }
    public long Completed { get; init; }
    public long Cancelled { get; init; }
    public double AveragePrice { get; init; }
    public double AverageDistanceMeters { get; init; }
}

public sealed class RatingStats
{
    public double AverageDriverRating { get; init; }
    public double AverageUserRating { get; init; }
    public long RatedTrips { get; init; }
}
