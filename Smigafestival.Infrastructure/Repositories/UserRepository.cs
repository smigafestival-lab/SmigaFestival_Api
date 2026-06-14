using Microsoft.EntityFrameworkCore;
using Smigafestival.Application.Abstractions;
using Smigafestival.Domain.Entities;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> FindByEmailOrMobileAsync(string loginIdentifier, CancellationToken cancellationToken)
    {
        var normalizedLoginIdentifier = loginIdentifier.Trim().ToUpperInvariant();

        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.NormalizedEmail == normalizedLoginIdentifier || user.NormalizedMobileNumber == normalizedLoginIdentifier,
                cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> MobileExistsAsync(string normalizedMobileNumber, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(user => user.NormalizedMobileNumber == normalizedMobileNumber, cancellationToken);
    }

    public Task AddAsync(AppUser user, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
