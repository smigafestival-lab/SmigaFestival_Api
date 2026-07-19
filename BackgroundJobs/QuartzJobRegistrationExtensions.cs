using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Smigafestival.BackgroundJobs;

public static class QuartzJobRegistrationExtensions
{
    public static void AddPlanExpiryQuartz(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("JobSettings:PlanExpiryJob");
        var intervalMinutes = section.GetValue<int>("IntervalMinutes", 5);

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("plan-expiry-job");

            q.AddJob<PlanExpiryJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("plan-expiry-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(intervalMinutes)
                    .RepeatForever()));

            // Let Quartz resolve IJob via DI
            q.UseMicrosoftDependencyInjectionJobFactory();
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }
}

