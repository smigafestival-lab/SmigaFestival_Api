using Microsoft.Extensions.Hosting;

namespace Smigafestival.BackgroundJobs;

// Optional placeholder to keep hosting model consistent (QuartzHostedService is used instead).
// This file can be removed if not needed.
public sealed class PlanExpiryJobHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

