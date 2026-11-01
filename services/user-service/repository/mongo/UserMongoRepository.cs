using MongoDB.Bson;
using MongoDB.Driver;
using PolesTaxi.Shared.Errors;
using PolesTaxi.Shared.Types;
using PolesTaxi.UserService.Entity.Repository;
using PolesTaxi.UserService.Entity.Service;

namespace PolesTaxi.UserService.Repository.Mongo;

public sealed class UserMongoRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _users;

    public UserMongoRepository(IMongoCollection<UserDocument> users)
    {
        _users = users;
    }

    public async Task<User> Create(User user, CancellationToken cancellationToken)
    {
        var doc = ToDocument(user);
        try
        {
            await _users.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            throw AppException.Conflict("email or phone already exists");
        }

        return ToDomain(doc);
    }

    public async Task<User> GetById(string id, CancellationToken cancellationToken)
    {
        var doc = await _users.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return RequireActive(doc);
    }

    public async Task<User> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var doc = await _users.Find(x => x.Email == email).FirstOrDefaultAsync(cancellationToken);
        return RequireActive(doc);
    }

    public async Task<User> GetByPhone(string phone, CancellationToken cancellationToken)
    {
        var doc = await _users.Find(x => x.Phone == phone).FirstOrDefaultAsync(cancellationToken);
        return RequireActive(doc);
    }

    public async Task<User> UpdateProfile(string id, UserProfilePatch patch, CancellationToken cancellationToken)
    {
        var update = Builders<UserDocument>.Update
            .Set(x => x.Name, patch.Name)
            .Set(x => x.Phone, patch.Phone)
            .Set(x => x.Email, patch.Email)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        UserDocument doc;
        try
        {
            var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, id)
                         & Builders<UserDocument>.Filter.Eq(x => x.IsDeleted, false);
            FindOneAndUpdateOptions<UserDocument, UserDocument> options = new()
            {
                ReturnDocument = ReturnDocument.After
            };
            doc = await _users.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            throw AppException.Conflict("email or phone already exists");
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            throw AppException.Conflict("email or phone already exists");
        }

        return RequireActive(doc);
    }

    public async Task UpdatePasswordHash(string id, string passwordHash, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateOneAsync(
            x => x.Id == id && !x.IsDeleted,
            Builders<UserDocument>.Update
                .Set(x => x.PasswordHash, passwordHash)
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw AppException.NotFound("user not found");
        }
    }

    public async Task SoftDelete(string id, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateOneAsync(
            x => x.Id == id && !x.IsDeleted,
            Builders<UserDocument>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw AppException.NotFound("user not found");
        }
    }

    public async Task<User> ReplaceRecentRatings(string id, IReadOnlyList<double> ratings, CancellationToken cancellationToken)
    {
        var list = ratings.TakeLast(20).ToList();
        var rating = list.Count == 0 ? 0 : list.Average();
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, id)
                     & Builders<UserDocument>.Filter.Eq(x => x.IsDeleted, false);
        FindOneAndUpdateOptions<UserDocument, UserDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After
        };
        var doc = await _users.FindOneAndUpdateAsync(
            filter,
            Builders<UserDocument>.Update
                .Set(x => x.RecentRatings, list)
                .Set(x => x.Rating, rating)
                .Set(x => x.UpdatedAt, DateTime.UtcNow),
            options,
            cancellationToken);

        return RequireActive(doc);
    }

    private static User RequireActive(UserDocument? doc)
    {
        if (doc is null || doc.IsDeleted)
        {
            throw AppException.NotFound("user not found");
        }

        return ToDomain(doc);
    }

    private static UserDocument ToDocument(User user)
    {
        return new UserDocument
        {
            Id = string.IsNullOrEmpty(user.Id) ? ObjectId.GenerateNewId().ToString() : user.Id,
            Name = user.Name,
            Phone = user.Phone,
            Email = user.Email.ToLowerInvariant(),
            PasswordHash = user.PasswordHash,
            Roles = user.Roles.Select(r => r.ToWire()).ToList(),
            Rating = user.Rating,
            RecentRatings = user.RecentRatings.ToList(),
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static User ToDomain(UserDocument doc)
    {
        return new User
        {
            Id = doc.Id,
            Name = doc.Name,
            Phone = doc.Phone,
            Email = doc.Email,
            PasswordHash = doc.PasswordHash,
            Roles = doc.Roles.Select(EnumMaps.RoleFromWire).ToList(),
            Rating = doc.Rating,
            RecentRatings = doc.RecentRatings,
            IsDeleted = doc.IsDeleted,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt
        };
    }
}
