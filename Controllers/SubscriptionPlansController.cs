using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Constants;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public SubscriptionPlansController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Accessible by both User and Admin
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAllPlans(CancellationToken cancellationToken)
    {
        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(p => p.PlanId)
            .Select(p => new
            {
                p.PlanId,
                p.PlanAmount,
                p.PlanDuration,
                p.PlanCategory
            })
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }
}

