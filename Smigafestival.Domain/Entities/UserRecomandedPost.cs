namespace Smigafestival.Domain.Entities;

public sealed class UserRecomandedPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? PostUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
