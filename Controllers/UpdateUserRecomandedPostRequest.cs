using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Smigafestival.Controllers;

public sealed class UpdateUserRecomandedPostRequest
{
    [Required]
    [FromForm(Name = "userId")]
    public Guid UserId { get; set; }

    [Required]
    [FromForm(Name = "description")]
    public string Description { get; set; } = string.Empty;

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
}
