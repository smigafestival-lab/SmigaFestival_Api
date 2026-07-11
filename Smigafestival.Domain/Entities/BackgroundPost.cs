namespace Smigafestival.Domain.Entities;

public sealed class BackgroundPost
{
    public Guid PostId { get; set; } = Guid.NewGuid();

    public string PostName { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public DateTime PostShowDate { get; set; }

    public string PostUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Category? Category { get; set; }
}
