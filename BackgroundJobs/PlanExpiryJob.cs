using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.BackgroundJobs;

public sealed class PlanExpiryJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PlanExpiryJob> _logger;

    public PlanExpiryJob(AppDbContext dbContext, ILogger<PlanExpiryJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // Requirement: compare using UTC date
        var todayUtc = DateTime.UtcNow.Date;

        // Set isPlanExpire = true when PlanEndDate is today or before today.
        // Set isPlanExpire = false when PlanEndDate is after today.
        // (If PlanEndDate is null, we leave it as-is.)

        // Expired: PlanEndDate.Date <= todayUtc
        var expiredUsers = await _dbContext.Users
            .Where(u => u.PlanEndDate.HasValue && u.PlanEndDate.Value.Date <= todayUtc)
            .ToListAsync(context.CancellationToken);

        foreach (var user in expiredUsers)
        {
            if (!user.isPlanExpire)
                user.isPlanExpire = true;
        }

        // Not expired: PlanEndDate.Date > todayUtc
        var activeUsers = await _dbContext.Users
            .Where(u => u.PlanEndDate.HasValue && u.PlanEndDate.Value.Date > todayUtc)
            .ToListAsync(context.CancellationToken);

        foreach (var user in activeUsers)
        {
            if (user.isPlanExpire)
                user.isPlanExpire = false;
        }

        var affected = expiredUsers.Count + activeUsers.Count;
        _logger.LogInformation("PlanExpiryJob executed. Updated users count: {Affected}", affected);

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

