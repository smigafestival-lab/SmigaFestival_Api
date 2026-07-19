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
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var useGuestPosts = await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken);

        if (useGuestPosts)
        {
            var guestPosts = await _dbContext.GuestUserPosts
                .AsNoTracking()
                .OrderByDescending(post => post.CreatedAt)
                .Select(post => new
                {
                    post.PostId,
                    post.PostName,
                    post.PostUrl,
                    post.PostShowDate,
                    post.CreatedAt,
                    post.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return Ok(guestPosts);
        }

        var backgroundPosts = await _dbContext.BackgroundPosts
            .AsNoTracking()
            .Include(post => post.Category)
            .OrderByDescending(post => post.CreatedAt)
            .Select(post => new
            {
                post.PostId,
                post.PostName,
                post.CategoryId,
                CategoryName = post.Category != null ? post.Category.CategoryName : null,
                post.PostUrl,
                post.PostShowDate,
                post.CreatedAt,
                post.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(backgroundPosts);
    }

    // NOTE: Favorites-only response is implemented in BackgroundPostFavoritesController.



    [HttpGet("Category/{CategoryId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]

    public async Task<IActionResult> GetByCategoryId(Guid CategoryId,CancellationToken cancellationToken)
    {
        var useGuestPosts = await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken);

        if (useGuestPosts)
        {
            var guestPosts = await _dbContext.GuestUserPosts
                .AsNoTracking()
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.PostId,
                    item.PostName,
                    item.PostUrl,
                    item.PostShowDate,
                    item.CreatedAt,
                    item.UpdatedAt,
                })
                .ToListAsync(cancellationToken);

            return guestPosts.Count == 0 ? NotFound() : Ok(guestPosts);
        }

        var backgroundPosts = await _dbContext.BackgroundPosts
            .AsNoTracking()
            .Where(item => item.CategoryId == CategoryId)
            .Select(item => new
            {
                item.PostId,
                item.PostName,
                item.CategoryId,
                CategoryName = item.CategoryId == null ? null : (string?)null,
                item.PostUrl,
                item.PostShowDate,
                item.CreatedAt,
                item.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return backgroundPosts.Count == 0 ? NotFound() : Ok(backgroundPosts);
    }

    [HttpGet("{postId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetById(Guid postId, CancellationToken cancellationToken)
    {
        var useGuestPosts = await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken);

        if (useGuestPosts)
        {
            var guestPost = await _dbContext.GuestUserPosts
                .AsNoTracking()
                .Where(item => item.PostId == postId)
                .Select(item => new
                {
                    item.PostId,
                    item.PostName,
                    item.PostUrl,
                    item.PostShowDate,
                    item.CreatedAt,
                    item.UpdatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return guestPost is null ? NotFound() : Ok(guestPost);
        }

        var backgroundPost = await _dbContext.BackgroundPosts
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.PostId == postId)
            .Select(item => new
            {
                item.PostId,
                item.PostName,
                item.CategoryId,
                CategoryName = item.Category != null ? item.Category.CategoryName : null,
                item.PostUrl,
                item.PostShowDate,
                item.CreatedAt,
                item.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return backgroundPost is null ? NotFound() : Ok(backgroundPost);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] BackgroundPostUpsertRequest request, CancellationToken cancellationToken)
    {
        var validationMessage = await ValidateRequestAsync(request, cancellationToken, isUpdate: false);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        var now = DateTime.UtcNow;
        var files = GetFiles(request);
        var posts = new List<BackgroundPost>();

        foreach (var file in files)
        {
            var post = new BackgroundPost
            {
                CategoryId = request.CategoryId,
                PostShowDate = request.PostShowDate,
                PostName = request.PostName,
                PostUrl = await UploadFileAndGetUrlAsync(file, cancellationToken),
                CreatedAt = now,
                UpdatedAt = now,
            };

            posts.Add(post);
        }

        await _dbContext.BackgroundPosts.AddRangeAsync(posts, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(posts);
    }

    [HttpPut("{postId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid postId, [FromForm] BackgroundPostUpsertRequest request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.BackgroundPosts.FirstOrDefaultAsync(item => item.PostId == postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var validationMessage = await ValidateRequestAsync(request, cancellationToken, isUpdate: true);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        post.PostName = request.PostName;
        post.CategoryId = request.CategoryId;
        post.PostShowDate = request.PostShowDate;
        if (request.File is not null)
        {
            post.PostUrl = await UploadFileAndGetUrlAsync(request.File, cancellationToken);
        }

        post.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(post);
    }

    [HttpDelete("{postId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
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

    private async Task<string?> ValidateRequestAsync(
        BackgroundPostUpsertRequest request,
        CancellationToken cancellationToken,
        bool isUpdate)
    {
        var categoryExists = await _dbContext.Categories
            .AnyAsync(category => category.CategoryId == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return "CategoryId is invalid.";
        }

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
            return "Multiple file upload is only supported while creating background posts.";
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

        return result.sasUri.ToString();
    }

    private static List<IFormFile> GetFiles(BackgroundPostUpsertRequest request)
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

    private static string ResolveSinglePostName(BackgroundPostUpsertRequest request)
    {
        return request.PostName!.Trim();
    }
}
