using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Smigafestival.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=tcp:smigafestival.database.windows.net,1433;Initial Catalog=SmigaFestival_Dev;Persist Security Info=False;User ID=SmigaFestival_dev;Password=48304672@Jay;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
