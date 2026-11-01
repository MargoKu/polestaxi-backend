using FluentAssertions;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.App.Db.Mongo;
using PolesTaxi.UserService.Entity.Service;
using PolesTaxi.UserService.Repository.Mongo;
using Testcontainers.MongoDb;
using Xunit;

namespace PolesTaxi.UserService.Tests;

public sealed class MongoFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7").Build();
    public string ConnectionString => Container.GetConnectionString();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class UserMongoRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoFixture _fx;
    private int _db;

    public UserMongoRepositoryTests(MongoFixture fx) => _fx = fx;

    private async Task<UserMongoRepository> Repo()
    {
        _db++;
        var mongo = await MongoClient.New(_fx.ConnectionString, $"users_{_db}", CancellationToken.None);
        return new UserMongoRepository(mongo.Users);
    }

    private static User NewUser(string email, string phone) => new()
    {
        Id = string.Empty,
        Name = "Ann",
        Phone = phone,
        Email = email,
        PasswordHash = "hash",
        Roles = [Role.User],
        Rating = 0,
        RecentRatings = [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Create_and_get_by_email()
    {
        var repo = await Repo();
        var created = await repo.Create(NewUser("a@t.local", "+7111"), CancellationToken.None);
        created.Id.Should().NotBeNullOrEmpty();
        var loaded = await repo.GetByEmail("a@t.local", CancellationToken.None);
        loaded.Id.Should().Be(created.Id);
        loaded.Name.Should().Be("Ann");
    }

    [Fact]
    public async Task Duplicate_email_is_conflict()
    {
        var repo = await Repo();
        await repo.Create(NewUser("dup@t.local", "+7222"), CancellationToken.None);
        var act = () => repo.Create(NewUser("dup@t.local", "+7333"), CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(e => e.Code == ErrorCodes.Conflict);
    }

    [Fact]
    public async Task Soft_delete_hides_user()
    {
        var repo = await Repo();
        var created = await repo.Create(NewUser("del@t.local", "+7444"), CancellationToken.None);
        await repo.SoftDelete(created.Id, CancellationToken.None);
        var act = () => repo.GetById(created.Id, CancellationToken.None);
        await act.Should().ThrowAsync<AppException>().Where(e => e.Code == ErrorCodes.NotFound);
    }
}
