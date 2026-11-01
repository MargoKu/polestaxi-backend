using FluentAssertions;
using PolesTaxi.Shared.Errors;
using PolesTaxi.WalletService.App.Db.Pg;
using PolesTaxi.WalletService.Entity.Service;
using PolesTaxi.WalletService.Repository.Pg;
using Testcontainers.PostgreSql;
using Xunit;

namespace PolesTaxi.WalletService.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class WalletPgRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fx;

    public WalletPgRepositoryTests(PostgresFixture fx) => _fx = fx;

    private WalletPgRepository Repo()
    {
        var pg = PgClient.New(_fx.Container.GetConnectionString());
        return new WalletPgRepository(pg.DataSource);
    }

    [Fact]
    public async Task Create_and_get_wallet()
    {
        var repo = Repo();
        var created = await repo.CreateWallet($"u-{Guid.NewGuid():N}", "RUB", CancellationToken.None);
        var loaded = await repo.GetByUserId(created.UserId, CancellationToken.None);
        loaded.Balance.Should().Be(0);
        loaded.Currency.Should().Be("RUB");
    }

    [Fact]
    public async Task Duplicate_user_is_conflict()
    {
        var repo = Repo();
        var user = $"u-{Guid.NewGuid():N}";
        await repo.CreateWallet(user, "RUB", CancellationToken.None);
        var act = () => repo.CreateWallet(user, "RUB", CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(e => e.Code == ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Atomic_credit_then_debit()
    {
        var repo = Repo();
        var user = $"u-{Guid.NewGuid():N}";
        await repo.CreateWallet(user, "RUB", CancellationToken.None);
        await repo.ApplyMoney(user, WalletTxType.Credit, 100m, "o1", CancellationToken.None);
        await repo.ApplyMoney(user, WalletTxType.Debit, 40m, "o1", CancellationToken.None);
        var wallet = await repo.GetByUserId(user, CancellationToken.None);
        wallet.Balance.Should().Be(60m);
        var txs = await repo.ListTransactions(user, 10, CancellationToken.None);
        txs.Should().HaveCount(2);
    }
}
