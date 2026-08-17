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
public sealed class PostsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    
    private readonly IBlobStorageService _blobStorageService;

    public PostsController(AppDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var posts = await _dbContext.Posts
            .AsNoTracking()
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(posts.Select(MapPostResponse));
    }

    [HttpGet("by-subscribed-user/{subscribedUserId}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetBySubscribedUserId(string subscribedUserId, CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var normalizedSubscribedUserId = subscribedUserId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSubscribedUserId))
        {
            return BadRequest(new { message = "SubscribedUserId is required." });
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.SubscribedUserId == normalizedSubscribedUserId, cancellationToken);

        if (!userExists)
        {
            return NotFound(new { message = "Subscribed user not found." });
        }

        var posts = await _dbContext.Posts
            .AsNoTracking()
            .Where(post => post.SubscribedUserId == normalizedSubscribedUserId)
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(posts.Select(MapPostResponse));
    }

    [HttpGet("{postId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetById(Guid postId, CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var post = await _dbContext.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);

        return post is null ? NotFound() : Ok(MapPostResponse(post));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] PostUpsertRequest request, CancellationToken cancellationToken)
    {

        var validationMessage = await ValidatePostRequestAsync(request, cancellationToken, false);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        
        var imageUrl = await ResolveImageUrlAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        var post = new Post
        {
            PostName = request.PostName.Trim(),
            SubscribedUserId = NormalizeSubscribedUserId(request.SubscribedUserId),
            ImageUrl = imageUrl,
            CreatedAt = now,
            UpdatedAt = now
        };



        await _dbContext.Posts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { postId = post.PostId }, MapPostResponse(post));
    }

    [HttpPut("{postId:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid postId, [FromForm] PostUpsertRequest request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var validationMessage = await ValidatePostRequestAsync(request, cancellationToken, true);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        post.PostName = request.PostName.Trim();
        post.SubscribedUserId = NormalizeSubscribedUserId(request.SubscribedUserId);
        post.UpdatedAt = DateTime.UtcNow;

        if (request.File is not null)
        {
            post.ImageUrl = await ResolveImageUrlAsync(request, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapPostResponse(post));
    }

    [HttpDelete("{postId:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin}")]
    public async Task<IActionResult> Delete(Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts.FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string?> ValidatePostRequestAsync(
        PostUpsertRequest request,
        CancellationToken cancellationToken,
        bool isUpdate)
    {
        if (string.IsNullOrWhiteSpace(request.PostName))
        {
            return "PostName is required.";
        }

        var subscribedUserId = NormalizeSubscribedUserId(request.SubscribedUserId);
        if (subscribedUserId is not null)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(user => user.SubscribedUserId == subscribedUserId, cancellationToken);

            if (!userExists)
            {
                return "SubscribedUserId is invalid.";
            }
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

    private async Task<string> ResolveImageUrlAsync(PostUpsertRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File!.OpenReadStream();
        var result = await _blobStorageService.UploadAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            cancellationToken);

        return result.BlobUri.ToString();
    }

    private static string? NormalizeSubscribedUserId(string? subscribedUserId)
    {
        return string.IsNullOrWhiteSpace(subscribedUserId) ? null : subscribedUserId.Trim();
    }

    private object MapPostResponse(Post post)
    {
        return new
        {
            post.PostId,
            post.PostName,
            post.CreatedAt,
            post.UpdatedAt,
            ImageUrl = AddSasToken(post.ImageUrl),
            post.SubscribedUserId,
        };
    }

    private string AddSasToken(string blobUrl)
    {
        return _blobStorageService.GetBlobSasUriForUrl(blobUrl, _blobStorageService.DefaultSasExpiry).ToString();
    }
}
