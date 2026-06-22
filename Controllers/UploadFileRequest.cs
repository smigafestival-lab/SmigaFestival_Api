using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class UploadFileRequest
{
    [Required]
    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }
}
