using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Application.Abstractions;
using Smigafestival.Domain.Entities;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BackgroundPostsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public BackgroundPostsController(AppDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var posts = await _dbContext.BackgroundPosts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAt)
            .Select(post => new
            {
                post.PostId,
                post.PostName,
                post.PostUrl,
                post.CreatedAt,
                post.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(posts);
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetById(Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.BackgroundPosts
            .AsNoTracking()
            .Where(item => item.PostId == postId)
            .Select(item => new
            {
                item.PostId,
                item.PostName,
                item.PostUrl,
                item.CreatedAt,
                item.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] BackgroundPostUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationMessage = ValidateRequest(request, isUpdate: false);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        var postUrl = await UploadFileAndGetUrlAsync(request.File!, cancellationToken);
        var now = DateTime.UtcNow;
        var post = new BackgroundPost
        {
            PostName = request.PostName.Trim(),
            PostUrl = postUrl,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _dbContext.BackgroundPosts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { postId = post.PostId }, post);
    }

    [HttpPut("{postId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid postId, [FromForm] BackgroundPostUpsertRequest request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.BackgroundPosts.FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var validationMessage = ValidateRequest(request, isUpdate: true);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        post.PostName = request.PostName.Trim();
        if (request.File is not null)
        {
            post.PostUrl = await UploadFileAndGetUrlAsync(request.File, cancellationToken);
        }

        post.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(post);
    }

    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> Delete(Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.BackgroundPosts.FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        _dbContext.BackgroundPosts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidateRequest(BackgroundPostUpsertRequest request, bool isUpdate)
    {
        if (string.IsNullOrWhiteSpace(request.PostName))
        {
            return "PostName is required.";
        }

        if (!isUpdate && request.File is null)
        {
            return "File is required.";
        }

        if (request.File is not null && request.File.Length == 0)
        {
            return "File must not be empty.";
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

        return result.Url.ToString();
    }
}
