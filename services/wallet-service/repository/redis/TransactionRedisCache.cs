using System.Text.Json;
using PolesTaxi.WalletService.Entity.Service;
using StackExchange.Redis;

namespace PolesTaxi.WalletService.Repository.Redis;

public sealed class TransactionRedisCache
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;

    public TransactionRedisCache(IDatabase db)
    {
        _db = db;
    }

    public async Task SetRecent(string userId, IReadOnlyList<WalletTransaction> items, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.StringSetAsync($"tx:{userId}", JsonSerializer.Serialize(items, Json), TimeSpan.FromMinutes(5));
    }

    public async Task<IReadOnlyList<WalletTransaction>?> GetRecent(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync($"tx:{userId}");
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<WalletTransaction>>(value!, Json);
    }

    public Task Invalidate(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.KeyDeleteAsync($"tx:{userId}");
    }
}
