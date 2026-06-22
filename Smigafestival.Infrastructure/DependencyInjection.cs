using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smigafestival.Application.Abstractions;
using Smigafestival.Application.Common.Models;
using Smigafestival.Infrastructure.Persistence;
using Smigafestival.Infrastructure.Repositories;
using Smigafestival.Infrastructure.Services;

namespace Smigafestival.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = new JwtOptions
        {
            Issuer = jwtSection[nameof(JwtOptions.Issuer)] ?? string.Empty,
            Audience = jwtSection[nameof(JwtOptions.Audience)] ?? string.Empty,
            SigningKey = jwtSection[nameof(JwtOptions.SigningKey)] ?? string.Empty,
            ExpiryMinutes = int.TryParse(jwtSection[nameof(JwtOptions.ExpiryMinutes)], out var expiryMinutes)
                ? expiryMinutes
                : 60
        };
        services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwtOptions));

        var blobStorageSection = configuration.GetSection(BlobStorageOptions.SectionName);
        var blobStorageOptions = new BlobStorageOptions
        {
            ConnectionString = blobStorageSection[nameof(BlobStorageOptions.ConnectionString)] ?? string.Empty,
            ContainerName = blobStorageSection[nameof(BlobStorageOptions.ContainerName)] ?? string.Empty
        };
        services.AddSingleton(blobStorageOptions);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        return services;
    }
}
