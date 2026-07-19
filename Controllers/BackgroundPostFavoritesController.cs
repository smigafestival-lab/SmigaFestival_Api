using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smigafestival.Domain.Constants;
using Smigafestival.Infrastructure.Persistence;

namespace Smigafestival.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BackgroundPostFavoritesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public BackgroundPostFavoritesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public sealed class UpsertFavoriteRequest
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public bool IsFaveroit { get; set; }
    }

    // Returns only PostId + IsFaveroit for a user (join with BackgroundPost).
    [HttpGet("UsersFaveroitpost/{userId:guid}")]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> UsersFaveroitpost(Guid userId, CancellationToken cancellationToken)
    {
        var favorites = await _dbContext.UsersFaveroitPosts
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.IsFaveroit)
            .Join(
                _dbContext.BackgroundPosts.AsNoTracking(),
                f => f.PostId,
                p => p.PostId,
                (f, p) => new
                {
                    p.PostId,
                    f.IsFaveroit,
                })
            .ToListAsync(cancellationToken);

        return Ok(favorites);
    }

    // Upsert favorite row for (UserId, PostId)
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.User},{AppRoles.Admin}")]
    public async Task<IActionResult> Upsert([FromBody] UpsertFavoriteRequest request, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UsersFaveroitPosts
            .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.PostId == request.PostId, cancellationToken);

        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
        }

        var postExists = await _dbContext.BackgroundPosts.AnyAsync(p => p.PostId == request.PostId, cancellationToken);
        if (!postExists)
        {
            return NotFound(new { message = "Post not found." });
        }

        if (existing is null)
        {
            var entity = new Smigafestival.Domain.Entities.UsersFaveroitPost
            {
                UserId = request.UserId,
                PostId = request.PostId,
                IsFaveroit = request.IsFaveroit,
            };

            _dbContext.UsersFaveroitPosts.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { entity.PostId, entity.UserId, entity.IsFaveroit, entity.Id });
        }

        existing.IsFaveroit = request.IsFaveroit;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { existing.PostId, existing.UserId, existing.IsFaveroit, existing.Id });
    }

}


