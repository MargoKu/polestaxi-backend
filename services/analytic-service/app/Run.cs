using PolesTaxi.AnalyticService.App.Db.ClickHouse;
using PolesTaxi.AnalyticService.Config;
using PolesTaxi.AnalyticService.Repository.ClickHouse;

namespace PolesTaxi.AnalyticService.App;

public static class Run
{
    public static async Task<int> ExecuteAsync(string[] args)
    {
        _ = args;
        var config = AppConfig.FromEnvironment();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var store = await ClickHouseStore.New(config.ClickHouseConnection, cts.Token);
            _ = new AnalyticClickHouseRepository(store.ConnectionString);
            Console.WriteLine("clickhouse ready");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"clickhouse init failed: {ex.Message}");
            return 1;
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        Console.WriteLine("shutdown");
        return 0;
    }
}
