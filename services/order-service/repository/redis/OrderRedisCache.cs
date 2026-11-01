using System.Text.Json;
using TaxiOrder = PolesTaxi.OrderService.Entity.Service.Order;
using PolesTaxi.OrderService.Entity.Service;
using StackExchange.Redis;

namespace PolesTaxi.OrderService.Repository.Redis;

public sealed class OrderRedisCache
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl;

    public OrderRedisCache(IDatabase db, TimeSpan ttl)
    {
        _db = db;
        _ttl = ttl;
    }

    public async Task Set(TaxiOrder order, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.StringSetAsync($"order:{order.Id}", JsonSerializer.Serialize(order, Json), _ttl);
    }

    public async Task<TaxiOrder?> Get(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await _db.StringGetAsync($"order:{id}");
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TaxiOrder>(value!, Json);
    }

    public async Task Invalidate(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync($"order:{id}");
    }
}
