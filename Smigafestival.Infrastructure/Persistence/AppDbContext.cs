using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Entities;

namespace Smigafestival.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.MobileNumber).HasMaxLength(20).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.NormalizedMobileNumber).HasMaxLength(20).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasIndex(user => user.NormalizedMobileNumber).IsUnique();
        });
    }
}
