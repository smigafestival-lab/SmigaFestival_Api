using Smigafestival.Application.Common.Models;
using Smigafestival.Domain.Entities;

namespace Smigafestival.Application.Abstractions;

public interface IJwtTokenService
{
    AuthResponse CreateToken(AppUser user);
}
