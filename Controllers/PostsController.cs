using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Application.Abstractions;
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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var posts = await _dbContext.Posts
            .AsNoTracking()
            .Include(post => post.Category)
            .OrderByDescending(post => post.PostShowDate)
            .Select(post => new
            {
                post.PostId,
                post.PostName,
                post.CreatedAt,
                post.UpdatedAt,
                post.ImageUrl,
                post.CategoryId,
                CategoryName = post.Category != null ? post.Category.CategoryName : null,
                post.PostShowDate,
                post.IsSpecial
            })
            .ToListAsync(cancellationToken);

        return Ok(posts);
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetById(Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.PostId == postId)
            .Select(item => new
            {
                item.PostId,
                item.PostName,
                item.CreatedAt,
                item.UpdatedAt,
                item.ImageUrl,
                item.CategoryId,
                CategoryName = item.Category != null ? item.Category.CategoryName : null,
                item.PostShowDate,
                item.IsSpecial
            })
            .FirstOrDefaultAsync(cancellationToken);

        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
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
            CategoryId = request.CategoryId,
            PostShowDate = request.PostShowDate,
            IsSpecial = request.IsSpecial,
            ImageUrl = imageUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.Posts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { postId = post.PostId }, post);
    }

    [HttpPut("{postId:guid}")]
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
        post.CategoryId = request.CategoryId;
        post.PostShowDate = request.PostShowDate;
        post.IsSpecial = request.IsSpecial;
        post.UpdatedAt = DateTime.UtcNow;

        if (request.File is not null || !string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            post.ImageUrl = await ResolveImageUrlAsync(request, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(post);
    }

    [HttpDelete("{postId:guid}")]
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

        var categoryExists = await _dbContext.Categories
            .AnyAsync(category => category.CategoryId == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return "CategoryId is invalid.";
        }

        if (!isUpdate && request.File is null && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return "Either a file or ImageUrl is required.";
        }

        return null;
    }

    private async Task<string> ResolveImageUrlAsync(PostUpsertRequest request, CancellationToken cancellationToken)
    {
        if (request.File is not null && request.File.Length > 0)
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _blobStorageService.UploadAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                cancellationToken);

            return result.Url.ToString();
        }

        return request.ImageUrl!.Trim();
    }
}
