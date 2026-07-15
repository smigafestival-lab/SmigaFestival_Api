namespace Smigafestival.Domain.Entities;

public sealed class UsersFaveroitPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid PostId { get; set; }

    // When this row exists, the post is considered favorite for the user.
    public bool IsFaveroit { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

