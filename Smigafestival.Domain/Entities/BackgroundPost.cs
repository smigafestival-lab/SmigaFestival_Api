namespace Smigafestival.Domain.Entities;

public sealed class BackgroundPost
{
    public Guid PostId { get; set; } = Guid.NewGuid();

    public string PostName { get; set; } = string.Empty;

    public string PostUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
