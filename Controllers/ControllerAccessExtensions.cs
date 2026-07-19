using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Constants;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

internal static class ControllerAccessExtensions
{
    public static bool IsAdmin(this ControllerBase controller)
    {
        return controller.User.IsInRole(AppRoles.Admin);
    }

    public static async Task<bool> IsCurrentUserExpiredAsync(
        this ControllerBase controller,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (controller.IsAdmin())
        {
            return false;
        }

        var userIdValue = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return false;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.isPlanExpire)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static IActionResult ExpiredUserEmptyResult(this ControllerBase controller)
    {
        return controller.Ok(Array.Empty<object>());
    }
}
