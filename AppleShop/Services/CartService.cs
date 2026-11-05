namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface ICartService
{
    Task<CartDto> GetAsync(Guid userId, CancellationToken ct = default);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto, CancellationToken ct = default);
    Task<CartDto> UpdateItemAsync(Guid userId, int itemId, UpdateCartItemDto dto, CancellationToken ct = default);
    Task<CartDto> RemoveItemAsync(Guid userId, int itemId, CancellationToken ct = default);
}

public sealed class CartService(AppDbContext db) : ICartService
{
    public async Task<CartDto> GetAsync(Guid userId, CancellationToken ct = default) =>
        Map(await LoadAsync(userId, ct));

    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto, CancellationToken ct = default)
    {
        var cart = await LoadAsync(userId, ct);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct)
                      ?? throw new AppException("Товар не знайдено.", 404);

        if (product.Stock <= 0) throw new AppException("Товар відсутній на складі.", 409);

        ProductVariant? variant = null;

        if (dto.VariantId is { } variantId)
        {
            variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == product.Id, ct)
                      ?? throw new AppException("Варіант товару не знайдено.", 404);

            if (variant.Stock <= 0) throw new AppException("Обраний варіант відсутній на складі.", 409);
        }

        var available = variant?.Stock ?? product.Stock;
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId && i.VariantId == dto.VariantId);

        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + dto.Quantity, available);
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = dto.ProductId,
                VariantId = dto.VariantId,
                Quantity = Math.Min(dto.Quantity, available)
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, int itemId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        var cart = await LoadAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw new AppException("Позицію кошика не знайдено.", 404);

        if (dto.Quantity <= 0)
        {
            db.CartItems.Remove(item);
        }
        else
        {
            var available = item.Variant?.Stock ?? item.Product!.Stock;
            item.Quantity = Math.Min(dto.Quantity, Math.Max(1, available));
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, int itemId, CancellationToken ct = default)
    {
        var cart = await LoadAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                   ?? throw new AppException("Позицію кошика не знайдено.", 404);

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(ct);

        return await GetAsync(userId, ct);
    }

    private async Task<Cart> LoadAsync(Guid userId, CancellationToken ct)
    {
        var cart = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product!).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);

        return cart;
    }

    private static CartDto Map(Cart cart)
    {
        var items = cart.Items
            .Where(i => i.Product is not null)
            .OrderBy(i => i.Id)
            .Select(i => i.Adapt<CartItemDto>())
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            TotalPrice = items.Sum(i => i.LineTotal),
            TotalQuantity = items.Sum(i => i.Quantity)
        };
    }
}