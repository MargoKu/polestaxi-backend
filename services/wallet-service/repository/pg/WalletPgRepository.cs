using Dapper;
using Npgsql;
using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.Entity.Repository;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository.Redis;

namespace PolesTaxi.WalletService.Repository.Pg;

public sealed class WalletPgRepository : IWalletRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly PgAtomicRepository _atomic;
    private readonly TransactionRedisCache? _cache;

    public WalletPgRepository(NpgsqlDataSource dataSource, TransactionRedisCache? cache = null)
    {
        _dataSource = dataSource;
        _atomic = new PgAtomicRepository(dataSource);
        _cache = cache;
    }

    public async Task<Wallet> CreateWallet(string userId, string currency, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO wallets (id, user_id, balance, currency)
            VALUES (@Id, @UserId, 0, @Currency)
            RETURNING id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt
            """;
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            var row = await conn.QuerySingleAsync<WalletRow>(sql, new
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Currency = currency
            });
            return ToWallet(row);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw AppException.Conflict("wallet already exists");
        }
    }

    public async Task<Wallet> GetByUserId(string userId, CancellationToken cancellationToken)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var row = await conn.QuerySingleOrDefaultAsync<WalletRow>(
            """
            SELECT id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt
            FROM wallets WHERE user_id = @userId
            """,
            new { userId });
        if (row is null)
        {
            throw AppException.NotFound("wallet not found");
        }

        return ToWallet(row);
    }

    public async Task<WalletTransaction> ApplyMoney(string userId, WalletTxType type, decimal amount, string? orderId, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw AppException.Validation("amount must be positive");
        }

        var tx = await _atomic.Start(cancellationToken);
        try
        {
            var conn = tx.Connection!;
            var wallet = await conn.QuerySingleOrDefaultAsync<WalletRow>(
                """
                SELECT id AS Id, user_id AS UserId, balance AS Balance, currency AS Currency, created_at AS CreatedAt
                FROM wallets WHERE user_id = @userId FOR UPDATE
                """,
                new { userId }, tx);

            if (wallet is null)
            {
                throw AppException.NotFound("wallet not found");
            }

            var delta = type == WalletTxType.Debit ? -amount : amount;
            var newBalance = wallet.Balance + delta;
            if (newBalance < 0)
            {
                throw AppException.Conflict("insufficient funds");
            }

            await conn.ExecuteAsync(
                "UPDATE wallets SET balance = @balance WHERE id = @id",
                new { balance = newBalance, id = wallet.Id }, tx);

            var row = await conn.QuerySingleAsync<TransactionRow>(
                """
                INSERT INTO transactions (id, wallet_id, type, amount, status, order_id)
                VALUES (@Id, @WalletId, @Type, @Amount, 'completed', @OrderId)
                RETURNING id AS Id, wallet_id AS WalletId, type AS Type, amount AS Amount, status AS Status, order_id AS OrderId, created_at AS CreatedAt
                """,
                new
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = TypeToWire(type),
                    Amount = amount,
                    OrderId = orderId
                }, tx);

            await _atomic.Finish(tx);
            if (_cache is not null)
            {
                await _cache.Invalidate(userId, cancellationToken);
            }

            return ToTx(row);
        }
        catch
        {
            await _atomic.Abort(tx);
            throw;
        }
    }

    public async Task<IReadOnlyList<WalletTransaction>> ListTransactions(string userId, int limit, CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            var cached = await _cache.GetRecent(userId, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var take = limit <= 0 ? 20 : Math.Min(limit, 100);
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<TransactionRow>(
            """
            SELECT t.id AS Id, t.wallet_id AS WalletId, t.type AS Type, t.amount AS Amount, t.status AS Status, t.order_id AS OrderId, t.created_at AS CreatedAt
            FROM transactions t
            JOIN wallets w ON w.id = t.wallet_id
            WHERE w.user_id = @userId
            ORDER BY t.created_at DESC
            LIMIT @take
            """,
            new { userId, take });
        var list = rows.Select(ToTx).ToList();
        if (_cache is not null)
        {
            await _cache.SetRecent(userId, list, cancellationToken);
        }

        return list;
    }

    private static Wallet ToWallet(WalletRow row) => new()
    {
        Id = row.Id,
        UserId = row.UserId,
        Balance = row.Balance,
        Currency = row.Currency.Trim(),
        CreatedAt = row.CreatedAt
    };

    private static WalletTransaction ToTx(TransactionRow row) => new()
    {
        Id = row.Id,
        WalletId = row.WalletId,
        Type = row.Type switch
        {
            "debit" => WalletTxType.Debit,
            "credit" => WalletTxType.Credit,
            "refund" => WalletTxType.Refund,
            _ => WalletTxType.Unspecified
        },
        Amount = row.Amount,
        Status = row.Status switch
        {
            "completed" => WalletTxStatus.Completed,
            "pending" => WalletTxStatus.Pending,
            "failed" => WalletTxStatus.Failed,
            "rolled_back" => WalletTxStatus.RolledBack,
            _ => WalletTxStatus.Unspecified
        },
        OrderId = row.OrderId,
        CreatedAt = row.CreatedAt
    };

    private static string TypeToWire(WalletTxType type) => type switch
    {
        WalletTxType.Debit => "debit",
        WalletTxType.Refund => "refund",
        _ => "credit"
    };
}
