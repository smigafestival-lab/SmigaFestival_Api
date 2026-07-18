using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Smigafestival.Controllers;

public sealed class CreateUserRecomandedPostRequest
{
    [Required]
    [FromForm(Name = "description")]
    public string Description { get; set; } = string.Empty;
}
