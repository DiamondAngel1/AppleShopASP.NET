namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CategoryUpsertFormDto dto, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, CategoryUpsertFormDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CategoryService(AppDbContext db, IImageService images) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().OrderBy(c => c.Id).ProjectToType<CategoryDto>().ToListAsync(ct);

    public async Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().Where(c => c.Slug == slug).ProjectToType<CategoryDto>().FirstOrDefaultAsync(ct)
        ?? throw new AppException("Категорію не знайдено.", 404);

    public async Task<CategoryDto> CreateAsync(CategoryUpsertFormDto dto, CancellationToken ct = default)
    {
        var category = new Category
        {
            Name = dto.Name.Trim(),
            Slug = await UniqueSlugAsync(dto.Slug ?? dto.Name, null, ct)
        };

        if (dto.Image is not null)
            category.ImageUrl = (await images.SaveCategoryImageAsync(dto.Image, ct)).Url;

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return category.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpsertFormDto dto, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new AppException("Категорію не знайдено.", 404);

        if (dto.Image is not null)
        {
            var stored = await images.SaveCategoryImageAsync(dto.Image, ct);
            images.Delete(category.ImageUrl);
            category.ImageUrl = stored.Url;
        }

        category.Name = dto.Name.Trim();
        category.Slug = await UniqueSlugAsync(dto.Slug ?? dto.Name, id, ct);

        await db.SaveChangesAsync(ct);
        return category.Adapt<CategoryDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new AppException("Категорію не знайдено.", 404);

        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new AppException("Не можна видалити категорію, у якій є товари.", 409);

        images.Delete(category.ImageUrl);
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> UniqueSlugAsync(string source, int? excludeId, CancellationToken ct)
    {
        var baseSlug = Slugger.Create(source);
        var slug = baseSlug;
        var counter = 1;

        while (await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }
}