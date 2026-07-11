namespace Smigafestival.Domain.Entities;

public sealed class Post
{
    public Guid PostId { get; set; } = Guid.NewGuid();

    public string PostName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime PostShowDate { get; set; }

    public string? SubscribedUserId { get; set; }

    public bool IsFavorite { get; set; }
}
