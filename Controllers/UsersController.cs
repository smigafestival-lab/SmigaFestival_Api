using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Smigafestival.Application.Common.Models;
using Smigafestival.Domain.Constants;
using Smigafestival.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public UsersController(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAtUtc)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Role,
                user.MobileNumber,
                user.Address,
                user.Website,
                user.Email,
                user.SubscribedUserId,
                user.IsPaymentDone,
                user.CreatedAtUtc,
                user.BusinessName,
                
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("{userId:guid}/subscribed-user-id")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateSubscribedUserId(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.SubscribedUserId = await GenerateUniqueSubscribedUserIdAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            user.Id,
            user.SubscribedUserId
        });
    }

    [HttpDelete("{userId:guid}/subscribed-user-id")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteSubscribedUserId(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.SubscribedUserId = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("deserialize-token")]
    public async Task<IActionResult> DeserializeToken(
        [FromBody] DeserializeTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        ClaimsPrincipal principal;

        try
        {
            principal = ValidateToken(request.Token.Trim());
        }
        catch (SecurityTokenException)
        {
            return Unauthorized(new { message = "Invalid or expired token." });
        }
        catch (ArgumentException)
        {
            return Unauthorized(new { message = "Invalid or expired token." });
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Token does not contain a valid user id." });
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new
            {
                item.Id,
                item.FirstName,
                item.LastName,
                item.Role,
                item.MobileNumber,
                item.Address,
                item.Website,
                item.Email,
                item.SubscribedUserId,
                item.IsPaymentDone,
                item.CreatedAtUtc,
                item.BusinessName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? NotFound(new { message = "User not found." })
            : Ok(user);
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }

    private async Task<string> GenerateUniqueSubscribedUserIdAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidate = $"SUB-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
            var exists = await _dbContext.Users
                .AnyAsync(item => item.SubscribedUserId == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }
    }
}
