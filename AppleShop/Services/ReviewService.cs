namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetForProductAsync(int productId, int page, int pageSize, CancellationToken ct = default);
    Task<ReviewDto> CreateAsync(int productId, Guid userId, CreateReviewDto dto, CancellationToken ct = default);
}

public sealed class ReviewService(AppDbContext db) : IReviewService
{
    public async Task<PagedResult<ReviewDto>> GetForProductAsync(int productId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 10 : pageSize;

        var query = db.Reviews.AsNoTracking().Where(r => r.ProductId == productId);
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<ReviewDto>()
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(items, page, pageSize, totalCount);
    }

    public async Task<ReviewDto> CreateAsync(int productId, Guid userId, CreateReviewDto dto, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new AppException("Товар не знайдено.", 404);

        if (await db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId, ct))
            throw new AppException("Ви вже залишили відгук про цей товар.", 409);

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim()
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return await db.Reviews.AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.Id == review.Id)
            .ProjectToType<ReviewDto>()
            .FirstAsync(ct);
    }
}