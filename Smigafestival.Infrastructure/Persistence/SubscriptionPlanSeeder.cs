using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Entities;

namespace Smigafestival.Infrastructure.Persistence;

public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        const string alterUsersTableSql = """
            IF COL_LENGTH('dbo.Users', 'PlanID') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [PlanID] INT NOT NULL CONSTRAINT [DF_Users_PlanID] DEFAULT(0);
            END

            IF COL_LENGTH('dbo.Users', 'PlanStartDate') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [PlanStartDate] datetime2 NULL;
            END

            IF COL_LENGTH('dbo.Users', 'PlanEndDate') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Users] ADD [PlanEndDate] datetime2 NULL;
            END
            """;

        const string createTableSql = """
            IF OBJECT_ID(N'[dbo].[SubscriptionPlan]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SubscriptionPlan]
                (
                    [PlanId] INT NOT NULL PRIMARY KEY,
                    [PlanAmount] DECIMAL(18,2) NOT NULL,
                    [PlanDuration] INT NOT NULL,
                    [PlanCategory] NVARCHAR(50) NOT NULL
                );
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(alterUsersTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(createTableSql, cancellationToken);

        var hasAnyPlans = await dbContext.SubscriptionPlans.AnyAsync(cancellationToken);
        if (hasAnyPlans)
        {
            return;
        }

        var plans = new List<SubscriptionPlan>
        {
            new() { PlanId = 1, PlanAmount = 79m, PlanDuration = 1, PlanCategory = "Basic" },
            new() { PlanId = 2, PlanAmount = 199m, PlanDuration = 3, PlanCategory = "Basic" },
            new() { PlanId = 3, PlanAmount = 399m, PlanDuration = 6, PlanCategory = "Basic" },
            new() { PlanId = 4, PlanAmount = 599m, PlanDuration = 12, PlanCategory = "Basic" },
            new() { PlanId = 5, PlanAmount = 299m, PlanDuration = 3, PlanCategory = "Premium" },
            new() { PlanId = 6, PlanAmount = 499m, PlanDuration = 6, PlanCategory = "Premium" },
            new() { PlanId = 7, PlanAmount = 899m, PlanDuration = 12, PlanCategory = "Premium" }
        };

        await dbContext.SubscriptionPlans.AddRangeAsync(plans, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
