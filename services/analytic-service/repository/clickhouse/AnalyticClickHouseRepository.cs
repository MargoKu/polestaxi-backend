using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.Shared.Types;

namespace PolesTaxi.AnalyticService.Repository.ClickHouse;

public sealed class AnalyticClickHouseRepository : IAnalyticRepository
{
    private readonly string _connectionString;

    public AnalyticClickHouseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertUserRegistered(UserRegisteredFact fact, CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO user_registered (user_id, role, event_time) VALUES (@u, @r, @t)";
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "u", Value = fact.UserId });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "r", Value = fact.Role.ToWire() });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "t", Value = fact.EventTime.ToUniversalTime() });
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertOrderCompleted(OrderCompletedFact fact, CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO order_completed (order_id, user_id, driver_id, taxi_type, price, distance, event_time)
            VALUES (@oid, @uid, @did, @tt, @p, @d, @t)
            """;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "oid", Value = fact.OrderId });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "uid", Value = fact.UserId });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "did", Value = fact.DriverId ?? "" });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "tt", Value = fact.TaxiType.ToWire() });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "p", Value = fact.Price });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "d", Value = fact.DistanceMeters });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "t", Value = fact.EventTime.ToUniversalTime() });
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertTripRated(TripRatedFact fact, CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO trip_rated (order_id, from_role, score, event_time) VALUES (@oid, @r, @s, @t)";
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "oid", Value = fact.OrderId });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "r", Value = fact.FromRole });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "s", Value = fact.Score });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "t", Value = fact.EventTime.ToUniversalTime() });
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<OrderStats> GetOrderStats(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                count() AS total,
                countIf(1) AS completed,
                0 AS cancelled,
                if(isNaN(avg(price)), 0, avg(price)) AS avg_price,
                if(isNaN(avg(distance)), 0, avg(distance)) AS avg_distance
            FROM order_completed
            WHERE event_time >= @from AND event_time <= @to
            """;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "from", Value = from.ToUniversalTime() });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "to", Value = to.ToUniversalTime() });
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OrderStats();
        }

        return new OrderStats
        {
            TotalOrders = Convert.ToInt64(reader["total"]),
            Completed = Convert.ToInt64(reader["completed"]),
            Cancelled = Convert.ToInt64(reader["cancelled"]),
            AveragePrice = Convert.ToDouble(reader["avg_price"]),
            AverageDistanceMeters = Convert.ToDouble(reader["avg_distance"])
        };
    }

    public async Task<RatingStats> GetRatingStats(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                if(isNaN(avgIf(score, from_role = 'Driver')), 0, avgIf(score, from_role = 'Driver')) AS avg_driver,
                if(isNaN(avgIf(score, from_role = 'User')), 0, avgIf(score, from_role = 'User')) AS avg_user,
                count() AS rated
            FROM trip_rated
            WHERE event_time >= @from AND event_time <= @to
            """;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "from", Value = from.ToUniversalTime() });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "to", Value = to.ToUniversalTime() });
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new RatingStats();
        }

        return new RatingStats
        {
            AverageDriverRating = Convert.ToDouble(reader["avg_driver"]),
            AverageUserRating = Convert.ToDouble(reader["avg_user"]),
            RatedTrips = Convert.ToInt64(reader["rated"])
        };
    }
}
