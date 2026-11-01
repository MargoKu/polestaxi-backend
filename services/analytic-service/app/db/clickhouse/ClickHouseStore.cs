using ClickHouse.Client.ADO;

namespace PolesTaxi.AnalyticService.App.Db.ClickHouse;

public sealed class ClickHouseStore
{
    public string ConnectionString { get; }

    private ClickHouseStore(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public static async Task<ClickHouseStore> New(string connectionString, CancellationToken cancellationToken)
    {
        var store = new ClickHouseStore(connectionString);
        await store.EnsureTables(cancellationToken);
        return store;
    }

    private async Task EnsureTables(CancellationToken cancellationToken)
    {
        await using var conn = new ClickHouseConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);
        foreach (var ddl in Ddl)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = ddl;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static readonly string[] Ddl =
    [
        """
        CREATE TABLE IF NOT EXISTS user_registered
        (
            user_id String,
            role LowCardinality(String),
            event_time DateTime64(3, 'UTC')
        )
        ENGINE = MergeTree
        ORDER BY (event_time, user_id)
        """,
        """
        CREATE TABLE IF NOT EXISTS order_completed
        (
            order_id String,
            user_id String,
            driver_id String,
            taxi_type LowCardinality(String),
            price Decimal(12, 2),
            distance Int64,
            event_time DateTime64(3, 'UTC')
        )
        ENGINE = MergeTree
        ORDER BY (event_time, order_id)
        """,
        """
        CREATE TABLE IF NOT EXISTS trip_rated
        (
            order_id String,
            from_role LowCardinality(String),
            score Int8,
            event_time DateTime64(3, 'UTC')
        )
        ENGINE = MergeTree
        ORDER BY (event_time, order_id)
        """
    ];
}
