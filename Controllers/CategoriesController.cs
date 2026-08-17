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

public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public CategoriesController(AppDbContext dbContext, IBlobStorageService blobStorageService)
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

        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);

        return Ok(categories.Select(MapCategoryResponse));
    }

    [HttpGet("{categoryId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> GetById(Guid categoryId, CancellationToken cancellationToken)
    {
        if (await this.IsCurrentUserExpiredAsync(_dbContext, cancellationToken))
        {
            return this.ExpiredUserEmptyResult();
        }

        var category = await _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);

        return category is null ? NotFound() : Ok(MapCategoryResponse(category));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var categoryName = request.CategoryName.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { message = "CategoryName is required." });
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "Image file is required." });
        }

        var exists = await _dbContext.Categories
            .AnyAsync(category => category.CategoryName == categoryName, cancellationToken);

        if (exists)
        {
            return Conflict(new { message = "A category with the same name already exists." });
        }

        var imageUrl = await UploadFileAndGetUrlAsync(request.File, cancellationToken);
        var category = new Category
        {
            CategoryName = categoryName,
            ImageUrl = imageUrl,
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { categoryId = category.CategoryId }, MapCategoryResponse(category));
    }

    [HttpPut("{categoryId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid categoryId, [FromForm] CategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var categoryName = request.CategoryName.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { message = "CategoryName is required." });
        }

        var duplicateExists = await _dbContext.Categories
            .AnyAsync(item => item.CategoryId != categoryId && item.CategoryName == categoryName, cancellationToken);

        if (duplicateExists)
        {
            return Conflict(new { message = "A category with the same name already exists." });
        }

        category.CategoryName = categoryName;
        if (request.File is not null)
        {
            if (request.File.Length == 0)
            {
                return BadRequest(new { message = "Image file must not be empty." });
            }

            category.ImageUrl = await UploadFileAndGetUrlAsync(request.File, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapCategoryResponse(category));
    }

    [HttpDelete("{categoryId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var isUsedByPosts = await _dbContext.BackgroundPosts.AnyAsync(post => post.CategoryId == categoryId, cancellationToken);
        if (isUsedByPosts)
        {
            return Conflict(new { message = "This category is already linked to background posts and cannot be removed." });
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
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

    private object MapCategoryResponse(Category category)
    {
        return new
        {
            category.CategoryId,
            category.CategoryName,
            ImageUrl = AddSasToken(category.ImageUrl),
        };
    }

    private string AddSasToken(string blobUrl)
    {
        return _blobStorageService.GetBlobSasUriForUrl(blobUrl, _blobStorageService.DefaultSasExpiry).ToString();
    }
}
