using System.Data;
using Npgsql;

namespace PolesTaxi.WalletService.Repository.Pg;

public interface IPgAtomicRepository
{
    Task<NpgsqlTransaction> Start(CancellationToken cancellationToken);
    Task Finish(NpgsqlTransaction tx);
    Task Abort(NpgsqlTransaction tx);
}

public sealed class PgAtomicRepository : IPgAtomicRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PgAtomicRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<NpgsqlTransaction> Start(CancellationToken cancellationToken)
    {
        var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await conn.BeginTransactionAsync(cancellationToken);
    }

    public async Task Finish(NpgsqlTransaction tx)
    {
        await tx.CommitAsync();
        if (tx.Connection is not null)
        {
            await tx.Connection.DisposeAsync();
        }
    }

    public async Task Abort(NpgsqlTransaction tx)
    {
        try
        {
            if (tx.Connection?.State == ConnectionState.Open)
            {
                await tx.RollbackAsync();
            }
        }
        catch
        {
            // ignore already-done tx
        }

        if (tx.Connection is not null)
        {
            await tx.Connection.DisposeAsync();
        }
    }
}
