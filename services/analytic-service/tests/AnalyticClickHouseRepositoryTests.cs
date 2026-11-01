using FluentAssertions;
using PolesTaxi.AnalyticService.App.Db.ClickHouse;
using PolesTaxi.AnalyticService.Entity.Service;
using PolesTaxi.AnalyticService.Repository.ClickHouse;
using PolesTaxi.Shared.Types;
using Testcontainers.ClickHouse;
using Xunit;

namespace PolesTaxi.AnalyticService.Tests;

public sealed class ClickHouseFixture : IAsyncLifetime
{
    public ClickHouseContainer Container { get; } = new ClickHouseBuilder().WithImage("clickhouse/clickhouse-server:25.8").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class AnalyticClickHouseRepositoryTests : IClassFixture<ClickHouseFixture>
{
    private readonly ClickHouseFixture _fx;

    public AnalyticClickHouseRepositoryTests(ClickHouseFixture fx) => _fx = fx;

    private async Task<AnalyticClickHouseRepository> Repo()
    {
        var cs = _fx.Container.GetConnectionString();
        await ClickHouseStore.New(cs, CancellationToken.None);
        return new AnalyticClickHouseRepository(cs);
    }

    [Fact]
    public async Task Insert_and_aggregate()
    {
        var repo = await Repo();
        var now = DateTime.UtcNow;
        await repo.InsertOrderCompleted(new OrderCompletedFact
        {
            OrderId = Guid.NewGuid().ToString("N"),
            UserId = "u1",
            DriverId = "d1",
            TaxiType = TaxiType.Economy,
            Price = 100,
            DistanceMeters = 2000,
            EventTime = now
        }, CancellationToken.None);
        await repo.InsertTripRated(new TripRatedFact
        {
            OrderId = "o1",
            FromRole = "User",
            Score = 5,
            EventTime = now
        }, CancellationToken.None);

        await Task.Delay(500);
        var stats = await repo.GetOrderStats(now.AddHours(-1), now.AddHours(1), CancellationToken.None);
        stats.Completed.Should().BeGreaterThanOrEqualTo(1);
        stats.AveragePrice.Should().BeApproximately(100, 0.01);
        var ratings = await repo.GetRatingStats(now.AddHours(-1), now.AddHours(1), CancellationToken.None);
        ratings.RatedTrips.Should().BeGreaterThanOrEqualTo(1);
        ratings.AverageUserRating.Should().BeApproximately(5, 0.01);
    }
}
