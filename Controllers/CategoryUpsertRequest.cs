using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smigafestival.Controllers;

public sealed class CategoryUpsertRequest
{
    [FromForm(Name = "categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }
}
