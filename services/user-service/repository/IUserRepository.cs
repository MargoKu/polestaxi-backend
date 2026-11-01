using PolesTaxi.UserService.Entity.Service;

namespace PolesTaxi.UserService.Repository;

public interface IUserRepository
{
    Task<User> Create(User user, CancellationToken cancellationToken);
    Task<User> GetById(string id, CancellationToken cancellationToken);
    Task<User> GetByEmail(string email, CancellationToken cancellationToken);
    Task<User> GetByPhone(string phone, CancellationToken cancellationToken);
    Task<User> UpdateProfile(string id, UserProfilePatch patch, CancellationToken cancellationToken);
    Task UpdatePasswordHash(string id, string passwordHash, CancellationToken cancellationToken);
    Task SoftDelete(string id, CancellationToken cancellationToken);
    Task<User> ReplaceRecentRatings(string id, IReadOnlyList<double> ratings, CancellationToken cancellationToken);
}
