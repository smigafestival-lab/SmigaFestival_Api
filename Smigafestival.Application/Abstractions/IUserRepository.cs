using Smigafestival.Domain.Entities;

namespace Smigafestival.Application.Abstractions;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailOrMobileAsync(string loginIdentifier, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> MobileExistsAsync(string normalizedMobileNumber, CancellationToken cancellationToken);

    Task AddAsync(AppUser user, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
