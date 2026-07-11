namespace Smigafestival.Domain.Entities;

public sealed class Category
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();

    public string CategoryName { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<BackgroundPost> BackgroundPosts { get; set; } = new List<BackgroundPost>();
}
