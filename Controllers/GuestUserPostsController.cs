using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Application.Abstractions;
using Smigafestival.Domain.Constants;
using Smigafestival.Domain.Entities;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GuestUserPostsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public GuestUserPostsController(AppDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var posts = await _dbContext.GuestUserPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(posts.Select(MapGuestPostResponse));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] GuestUserPostUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationMessage = await ValidateRequestAsync(request, cancellationToken, isUpdate: false);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        var now = DateTime.UtcNow;
        var files = GetFiles(request);
        var posts = new List<GuestUserPost>();

        foreach (var file in files)
        {
            var post = new GuestUserPost
            {
                PostShowDate = request.PostShowDate,
                PostName = request.PostName,
                PostUrl = await UploadFileAndGetUrlAsync(file, cancellationToken),
                CreatedAt = now,
                UpdatedAt = now,
            };

            posts.Add(post);
        }

        await _dbContext.GuestUserPosts.AddRangeAsync(posts, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(posts.Select(MapGuestPostResponse));
    }

    [HttpDelete("{guestuserpostid:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid guestuserpostid, CancellationToken cancellationToken)
    {
        var post = await _dbContext.GuestUserPosts.FirstOrDefaultAsync(x => x.PostId == guestuserpostid,cancellationToken);

        
        if(post == null)
        {
            return BadRequest();
        }

        _dbContext.GuestUserPosts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }



    private async Task<string?> ValidateRequestAsync(
        GuestUserPostUpsertRequest request,
        CancellationToken cancellationToken,
        bool isUpdate)
    {
        var files = GetFiles(request);
        if (!isUpdate && files.Count == 0)
        {
            return "At least one file is required.";
        }

        if (request.File is not null && request.Files is not null && request.Files.Count > 0)
        {
            return "Use either file or files in the request, not both.";
        }

        if (files.Any(file => file.Length == 0))
        {
            return "File must not be empty.";
        }

        if (isUpdate && request.Files is not null && request.Files.Count > 0)
        {
            return "Multiple file upload is only supported while creating guest user posts.";
        }

        return null;
    }

    private async Task<string> UploadFileAndGetUrlAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _blobStorageService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return result.BlobUri.ToString();
    }

    private static List<IFormFile> GetFiles(GuestUserPostUpsertRequest request)
    {
        var files = new List<IFormFile>();
        if (request.File is not null)
        {
            files.Add(request.File);
        }

        if (request.Files is not null)
        {
            files.AddRange(request.Files.Where(file => file is not null));
        }

        return files;
    }

    private object MapGuestPostResponse(GuestUserPost post)
    {
        return new
        {
            post.PostId,
            post.PostName,
            PostUrl = AddSasToken(post.PostUrl),
            post.PostShowDate,
            post.CreatedAt,
            post.UpdatedAt,
        };
    }

    private string AddSasToken(string blobUrl)
    {
        return _blobStorageService.GetBlobSasUriForUrl(blobUrl, _blobStorageService.DefaultSasExpiry).ToString();
    }
}

