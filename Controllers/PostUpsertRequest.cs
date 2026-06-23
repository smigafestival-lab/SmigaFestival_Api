using Microsoft.AspNetCore.Http;

namespace Smigafestival.Controllers;

public sealed class PostUpsertRequest
{
    public string PostName { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public DateTime PostShowDate { get; set; }

    public bool IsSpecial { get; set; }

    public IFormFile? File { get; set; }

    public string? ImageUrl { get; set; }
}
