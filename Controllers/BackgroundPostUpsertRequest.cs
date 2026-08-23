using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class BackgroundPostUpsertRequest
{
    [FromForm(Name = "postName")]
    public string? PostName { get; set; }

    [FromForm(Name = "categoryId")]
    public Guid CategoryId { get; set; }

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    [FromForm(Name = "files")]
    public List<IFormFile>? Files { get; set; }

    [FromForm(Name = "postShowDate")]
    public DateTime? PostShowDate { get; set; }
}
