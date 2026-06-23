namespace Smigafestival.Controllers;

public sealed class CategoryUpsertRequest
{
    public string CategoryName { get; set; } = string.Empty;

    public bool IsSpecial { get; set; }
}
