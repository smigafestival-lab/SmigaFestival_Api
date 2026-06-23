using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Entities;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CategoriesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .Select(category => new
            {
                category.CategoryId,
                category.CategoryName,
                category.IsSpecial
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{categoryId:guid}")]
    public async Task<IActionResult> GetById(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(item => item.CategoryId == categoryId)
            .Select(item => new
            {
                item.CategoryId,
                item.CategoryName,
                item.IsSpecial
            })
            .FirstOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        var categoryName = request.CategoryName.Trim();
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { message = "CategoryName is required." });
        }

        var exists = await _dbContext.Categories
            .AnyAsync(category => category.CategoryName == categoryName, cancellationToken);

        if (exists)
        {
            return Conflict(new { message = "A category with the same name already exists." });
        }

        var category = new Category
        {
            CategoryName = categoryName,
            IsSpecial = request.IsSpecial
        };

        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { categoryId = category.CategoryId }, category);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<IActionResult> Update(Guid categoryId, [FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
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
        category.IsSpecial = request.IsSpecial;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(category);
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var isUsedByPosts = await _dbContext.Posts.AnyAsync(post => post.CategoryId == categoryId, cancellationToken);
        if (isUsedByPosts)
        {
            return Conflict(new { message = "This category is already linked to posts and cannot be removed." });
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
