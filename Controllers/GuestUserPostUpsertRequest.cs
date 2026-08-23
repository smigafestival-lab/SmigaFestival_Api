using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class GuestUserPostUpsertRequest
{
    [FromForm(Name = "postName")]
    public string? PostName { get; set; }

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    [FromForm(Name = "files")]
    public List<IFormFile>? Files { get; set; }

    [FromForm(Name = "postShowDate")]
    public DateTime? PostShowDate { get; set; }
}
