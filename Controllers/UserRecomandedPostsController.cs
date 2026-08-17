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
public sealed class UserRecomandedPostsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public UserRecomandedPostsController(
        AppDbContext dbContext,
        IBlobStorageService blobStorageService)
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

        var posts = await _dbContext.UserRecomandedPosts
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.Description,
                item.PostUrl,
                item.CreatedAt,
                item.UpdatedAt,

                FirstName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),

                LastName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),

                BusinessName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.BusinessName)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return Ok(posts.Select(MapRecommendedPostResponse));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var post = await _dbContext.UserRecomandedPosts
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.Description,
                item.PostUrl,
                item.CreatedAt,
                item.UpdatedAt,

                FirstName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),

                LastName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),

                BusinessName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.BusinessName)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return post is null
            ? NotFound(new { message = "Recommended post not found." })
            : Ok(MapRecommendedPostResponse(post));
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
        }

        var posts = await _dbContext.UserRecomandedPosts
            .AsNoTracking()
            .Where(item => item.UserId == userId && !string.IsNullOrWhiteSpace(item.PostUrl))
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.Description,
                item.PostUrl,
                item.CreatedAt,
                item.UpdatedAt,

                FirstName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.FirstName)
                    .FirstOrDefault(),

                LastName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.LastName)
                    .FirstOrDefault(),

                BusinessName = _dbContext.Users
                    .Where(u => u.Id == item.UserId)
                    .Select(u => u.BusinessName)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return Ok(posts.Select(MapRecommendedPostResponse));
    }

    [HttpPost("{userId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        Guid userId,
        [FromForm] CreateUserRecomandedPostRequest request,
        CancellationToken cancellationToken)
    {
        var validationMessage = await ValidateCreateRequestAsync(userId, request, cancellationToken);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        var now = DateTime.UtcNow;

        var post = new UserRecomandedPost
        {
            UserId = userId,
            Description = request.Description.Trim(),
            PostUrl = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _dbContext.UserRecomandedPosts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, MapCreatedRecommendedPostResponse(post));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromForm] UpdateUserRecomandedPostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await _dbContext.UserRecomandedPosts
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (post is null)
        {
            return NotFound(new { message = "Recommended post not found." });
        }

        var validationMessage = await ValidateUpdateRequestAsync(request, cancellationToken);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        post.UserId = request.UserId;
        post.Description = request.Description.Trim();
        post.UpdatedAt = DateTime.UtcNow;

        if (request.File is not null)
        {
            post.PostUrl = await UploadFileAsync(request.File, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapCreatedRecommendedPostResponse(post));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var post = await _dbContext.UserRecomandedPosts
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (post is null)
        {
            return NotFound(new { message = "Recommended post not found." });
        }

        _dbContext.UserRecomandedPosts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string?> ValidateCreateRequestAsync(
        Guid userId,
        CreateUserRecomandedPostRequest request,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return "UserId is required.";
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return "User not found.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "Description is required.";
        }

        return null;
    }

    private async Task<string?> ValidateUpdateRequestAsync(
        UpdateUserRecomandedPostRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return "UserId is required.";
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return "User not found.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "Description is required.";
        }

        if (request.File is not null && request.File.Length == 0)
        {
            return "File must not be empty.";
        }

        return null;
    }

    private async Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _blobStorageService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return result.BlobUri.ToString();
    }

    private object MapCreatedRecommendedPostResponse(UserRecomandedPost post)
    {
        return new
        {
            post.Id,
            post.UserId,
            post.Description,
            PostUrl = AddSasTokenIfPresent(post.PostUrl),
            post.CreatedAt,
            post.UpdatedAt,
        };
    }

    private object MapRecommendedPostResponse(dynamic post)
    {
        return new
        {
            post.Id,
            post.UserId,
            post.Description,
            PostUrl = AddSasTokenIfPresent(post.PostUrl),
            post.CreatedAt,
            post.UpdatedAt,
            post.FirstName,
            post.LastName,
            post.BusinessName,
        };
    }

    private string? AddSasTokenIfPresent(string? blobUrl)
    {
        return string.IsNullOrWhiteSpace(blobUrl)
            ? blobUrl
            : _blobStorageService.GetBlobSasUriForUrl(blobUrl, _blobStorageService.DefaultSasExpiry).ToString();
    }
}
