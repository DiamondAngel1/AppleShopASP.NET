namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface IFavoriteService
{
    Task<PagedResult<ProductCardDto>> GetAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Guid userId, int productId, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, int productId, CancellationToken ct = default);
}

public sealed class FavoriteService(AppDbContext db) : IFavoriteService
{
    public async Task<PagedResult<ProductCardDto>> GetAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 60 ? 12 : pageSize;

        var query = db.Favorites.AsNoTracking().Where(f => f.UserId == userId);
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(f => f.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => f.Product!)
            .ProjectToType<ProductCardDto>()
            .ToListAsync(ct);

        return new PagedResult<ProductCardDto>(items, page, pageSize, totalCount);
    }

    public async Task AddAsync(Guid userId, int productId, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new AppException("Товар не знайдено.", 404);

        if (await db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId, ct)) return;

        db.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid userId, int productId, CancellationToken ct = default)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, ct);
        if (favorite is null) return;

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
    }
}