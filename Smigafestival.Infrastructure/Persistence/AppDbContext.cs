using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Constants;
using Smigafestival.Domain.Entities;

namespace Smigafestival.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<BackgroundPost> BackgroundPosts => Set<BackgroundPost>();
    public DbSet<UsersFaveroitPost> UsersFaveroitPosts => Set<UsersFaveroitPost>();
    public DbSet<UserRecomandedPost> UserRecomandedPosts => Set<UserRecomandedPost>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();


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
            entity.Property(user => user.BusinessName).IsRequired();
            entity.Property(user => user.SubscribedUserId).HasMaxLength(100);
            entity.Property(user => user.IsPaymentDone).HasDefaultValue(false);
            entity.Property(user => user.Role)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(AppRoles.User);
            entity.Property(user => user.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.Property(user => user.isPlanExpire).HasDefaultValue(false);
            entity.Property(user => user.PlanID).HasDefaultValue(0);
            entity.Property(user => user.PlanStartDate);
            entity.Property(user => user.PlanEndDate);

            entity.Property(user => user.Address).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Website).HasMaxLength(200);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasIndex(user => user.NormalizedMobileNumber).IsUnique();
            entity.HasIndex(user => user.SubscribedUserId).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.CategoryId);
            entity.Property(category => category.CategoryName).HasMaxLength(200).IsRequired();
            entity.Property(category => category.ImageUrl).HasMaxLength(2048).IsRequired();

            entity.HasIndex(category => category.CategoryName).IsUnique();
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(post => post.PostId);
            entity.Property(post => post.PostName).HasMaxLength(200).IsRequired();
            entity.Property(post => post.ImageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(post => post.SubscribedUserId).HasMaxLength(100);
            entity.Property(post => post.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(post => post.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(post => post.SubscribedUserId);
        });

        modelBuilder.Entity<BackgroundPost>(entity =>
        {
            entity.ToTable("BackgroundPost");
            entity.HasKey(post => post.PostId);
            entity.Property(post => post.PostName).HasMaxLength(200).IsRequired();
            entity.Property(post => post.PostUrl).HasMaxLength(2048).IsRequired();
            entity.Property(post => post.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(post => post.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(post => post.Category)
                .WithMany(category => category.BackgroundPosts)
                .HasForeignKey(post => post.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(post => post.CategoryId);
            entity.HasIndex(post => post.PostShowDate);
        });

        modelBuilder.Entity<UsersFaveroitPost>(entity =>
        {
            entity.ToTable("UsersFaveroitPost");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.PostId).IsRequired();

            entity.Property(x => x.IsFaveroit).HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
        });

        modelBuilder.Entity<UserRecomandedPost>(entity =>
        {
            entity.ToTable("UserRecomandedPost");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.PostUrl).HasMaxLength(2048);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("SubscriptionPlan");
            entity.HasKey(x => x.PlanId);

            entity.Property(x => x.PlanId).ValueGeneratedNever();
            entity.Property(x => x.PlanAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PlanDuration).IsRequired();
            entity.Property(x => x.PlanCategory).HasMaxLength(50).IsRequired();
        });
    }
}
