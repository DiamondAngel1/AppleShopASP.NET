using Microsoft.AspNetCore.Http;

namespace AppleShop.Services;

using System.Text.Json;
using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface IProductService
{
    Task<PagedResult<ProductCardDto>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDetailsDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<ProductDetailsDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetFeaturedAsync(int take, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardDto>> GetNewArrivalsAsync(int take, CancellationToken ct = default);
    Task<ProductDetailsDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDetailsDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<ProductImageDto>> AddImagesAsync(int productId, IEnumerable<IFormFile> files, CancellationToken ct = default);
    Task DeleteImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task SetMainImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductImageDto>> ReorderImagesAsync(int productId, ReorderImagesDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class ProductService(AppDbContext db, IImageService images) : IProductService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public async Task<PagedResult<ProductCardDto>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var source = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.ShortDescription, term));
        }

        if (query.CategoryId is { } categoryId) source = source.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.Category)) source = source.Where(p => p.Category!.Slug == query.Category);
        if (query.MinPrice is { } min) source = source.Where(p => p.Price >= min);
        if (query.MaxPrice is { } max) source = source.Where(p => p.Price <= max);
        if (!string.IsNullOrWhiteSpace(query.Color)) source = source.Where(p => p.Variants.Any(v => v.Color == query.Color));
        if (query.InStockOnly == true) source = source.Where(p => p.Stock > 0);

        var totalCount = await source.CountAsync(ct);

        source = query.Sort?.ToLowerInvariant() switch
        {
            "price-asc" => source.OrderBy(p => p.Price),
            "price-desc" => source.OrderByDescending(p => p.Price),
            "name" => source.OrderBy(p => p.Name),
            _ => source.OrderByDescending(p => p.CreatedAt)
        };

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectToType<ProductCardDto>()
            .ToListAsync(ct);

        return new PagedResult<ProductCardDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ProductDetailsDto> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await DetailsQuery().Where(p => p.Slug == slug).ProjectToType<ProductDetailsDto>().FirstOrDefaultAsync(ct)
        ?? throw new AppException("Товар не знайдено.", 404);

    public async Task<ProductDetailsDto> GetByIdAsync(int id, CancellationToken ct = default) =>
        await DetailsQuery().Where(p => p.Id == id).ProjectToType<ProductDetailsDto>().FirstOrDefaultAsync(ct)
        ?? throw new AppException("Товар не знайдено.", 404);

    public async Task<IReadOnlyList<ProductCardDto>> GetFeaturedAsync(int take, CancellationToken ct = default) =>
        await db.Products.AsNoTracking()
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(Clamp(take))
            .ProjectToType<ProductCardDto>()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductCardDto>> GetNewArrivalsAsync(int take, CancellationToken ct = default) =>
        await db.Products.AsNoTracking()
            .OrderByDescending(p => p.IsNew)
            .ThenByDescending(p => p.CreatedAt)
            .Take(Clamp(take))
            .ProjectToType<ProductCardDto>()
            .ToListAsync(ct);

    public async Task<ProductDetailsDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        await EnsureCategoryAsync(dto.CategoryId, ct);

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Slug = await UniqueSlugAsync(dto.Name, null, ct),
            ShortDescription = dto.ShortDescription.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            OldPrice = dto.OldPrice,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            IsFeatured = dto.IsFeatured,
            IsNew = dto.IsNew,
            Specs = ParseSpecs(dto.Specs),
            Variants = ParseVariants(dto.Variants)
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        if (dto.Images is { Count: > 0 }) await SaveImagesAsync(product.Id, dto.Images, ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDetailsDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Specs)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Товар не знайдено.", 404);

        await EnsureCategoryAsync(dto.CategoryId, ct);

        product.Name = dto.Name.Trim();
        product.Slug = await UniqueSlugAsync(dto.Name, id, ct);
        product.ShortDescription = dto.ShortDescription.Trim();
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.OldPrice = dto.OldPrice;
        product.Stock = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.IsFeatured = dto.IsFeatured;
        product.IsNew = dto.IsNew;
        product.UpdatedAt = DateTime.UtcNow;

        db.ProductSpecs.RemoveRange(product.Specs);
        db.ProductVariants.RemoveRange(product.Variants);

        product.Specs = ParseSpecs(dto.Specs);
        product.Variants = ParseVariants(dto.Variants);

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<ProductImageDto>> AddImagesAsync(int productId, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new AppException("Товар не знайдено.", 404);

        await SaveImagesAsync(productId, files.ToList(), ct);
        return await ImagesAsync(productId, ct);
    }

    public async Task DeleteImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct)
                    ?? throw new AppException("Зображення не знайдено.", 404);

        var wasMain = image.IsMain;

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(ct);

        images.Delete(image.Url);
        images.Delete(image.ThumbnailUrl);

        if (!wasMain) return;

        var next = await db.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.SortOrder)
            .FirstOrDefaultAsync(ct);

        if (next is null) return;

        next.IsMain = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetMainImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var list = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync(ct);
        if (list.Count == 0 || list.All(i => i.Id != imageId))
            throw new AppException("Зображення не знайдено.", 404);

        foreach (var image in list) image.IsMain = image.Id == imageId;

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductImageDto>> ReorderImagesAsync(int productId, ReorderImagesDto dto, CancellationToken ct = default)
    {
        var list = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync(ct);
        if (list.Count == 0) throw new AppException("У товару немає зображень.", 404);

        var position = 0;

        foreach (var imageId in dto.ImageIds)
        {
            var image = list.FirstOrDefault(i => i.Id == imageId)
                        ?? throw new AppException($"Зображення {imageId} не належить цьому товару.");

            image.SortOrder = position;
            image.IsMain = position == 0;
            position++;
        }

        foreach (var rest in list.Where(i => !dto.ImageIds.Contains(i.Id))) rest.SortOrder = position++;

        await db.SaveChangesAsync(ct);
        return await ImagesAsync(productId, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new AppException("Товар не знайдено.", 404);

        var urls = product.Images.SelectMany(i => new[] { i.Url, i.ThumbnailUrl }).ToList();

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        foreach (var url in urls) images.Delete(url);
    }

    private IQueryable<Product> DetailsQuery() => db.Products.AsNoTracking()
        .Include(p => p.Category)
        .Include(p => p.Images)
        .Include(p => p.Variants)
        .Include(p => p.Specs)
        .Include(p => p.Reviews);

    private async Task SaveImagesAsync(int productId, IReadOnlyList<IFormFile> files, CancellationToken ct)
    {
        if (files.Count == 0) return;

        var hasMain = await db.ProductImages.AnyAsync(i => i.ProductId == productId && i.IsMain, ct);
        var order = await db.ProductImages.Where(i => i.ProductId == productId).Select(i => (int?)i.SortOrder).MaxAsync(ct) ?? -1;

        foreach (var file in files)
        {
            var stored = await images.SaveProductImageAsync(file, ct);
            order++;

            db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = stored.Url,
                ThumbnailUrl = stored.ThumbnailUrl,
                IsMain = !hasMain,
                SortOrder = order
            });

            hasMain = true;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<ProductImageDto>> ImagesAsync(int productId, CancellationToken ct) =>
        await db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.IsMain)
            .ThenBy(i => i.SortOrder)
            .ProjectToType<ProductImageDto>()
            .ToListAsync(ct);

    private async Task EnsureCategoryAsync(int categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw new AppException("Категорію не знайдено.", 404);
    }

    private async Task<string> UniqueSlugAsync(string name, int? excludeId, CancellationToken ct)
    {
        var baseSlug = Slugger.Create(name);
        var slug = baseSlug;
        var counter = 1;

        while (await db.Products.AnyAsync(p => p.Slug == slug && p.Id != excludeId, ct))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }

    private static int Clamp(int take) => take is < 1 or > 24 ? 8 : take;

    private static List<ProductSpec> ParseSpecs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        return (JsonSerializer.Deserialize<List<Spec>>(json, Json) ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Key))
            .Select(s => new ProductSpec { Key = s.Key.Trim(), Value = s.Value?.Trim() ?? string.Empty })
            .ToList();
    }

    private static List<ProductVariant> ParseVariants(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        return (JsonSerializer.Deserialize<List<Variant>>(json, Json) ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v.Color))
            .Select(v => new ProductVariant
            {
                Color = v.Color.Trim(),
                Storage = v.Storage?.Trim() ?? string.Empty,
                PriceDelta = v.PriceDelta,
                Stock = Math.Max(0, v.Stock)
            })
            .ToList();
    }

    private sealed class Spec
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private sealed class Variant
    {
        public string Color { get; set; } = string.Empty;
        public string? Storage { get; set; }
        public decimal PriceDelta { get; set; }
        public int Stock { get; set; }
    }
}