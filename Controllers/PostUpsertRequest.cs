using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class PostUpsertRequest
{
    [Required]
    [FromForm(Name = "postName")]
    public string PostName { get; set; } = string.Empty;

    [FromForm(Name = "subscribedUserId")]
    public string? SubscribedUserId { get; set; }

    [FromForm(Name = "isFavorite")]
    public bool IsFavorite { get; set; }

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
}
