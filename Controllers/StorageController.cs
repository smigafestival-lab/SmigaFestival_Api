using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smigafestival.Application.Abstractions;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StorageController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;

    public StorageController(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    [AllowAnonymous]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "Please select a file to upload." });
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _blobStorageService.UploadAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            cancellationToken);

        return Ok(result);
    }
}
