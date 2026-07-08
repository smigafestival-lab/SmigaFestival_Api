namespace Smigafestival.Domain.Entities;

public sealed class Category
{
    public Guid CategoryId { get; set; } = Guid.NewGuid();

    public string CategoryName { get; set; } = string.Empty;


    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
