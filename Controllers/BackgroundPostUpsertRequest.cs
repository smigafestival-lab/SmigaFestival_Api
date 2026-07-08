using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class BackgroundPostUpsertRequest
{
    [Required]
    [FromForm(Name = "postName")]
    public string PostName { get; set; } = string.Empty;

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
}
