namespace Smigafestival.Application.Common.Models;

public sealed record AuthResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string MobileNumber,
    string Email,
    string Token,
    DateTime ExpiresAtUtc);
