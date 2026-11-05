namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Mapster;
using Microsoft.EntityFrameworkCore;

public interface IOrderService
{
    Task<OrderDto> CreateFromCartAsync(Guid userId, CreateOrderDto dto, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(Guid userId, int orderId, bool isAdmin, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> GetAllAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default);
    Task<OrderDto> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto, CancellationToken ct = default);
}

public sealed class OrderService(AppDbContext db) : IOrderService
{
    public async Task<OrderDto> CreateFromCartAsync(Guid userId, CreateOrderDto dto, CancellationToken ct = default)
    {
        var cart = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product!).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || cart.Items.Count == 0) throw new AppException("Кошик порожній.");

        foreach (var item in cart.Items)
            if (item.Quantity > (item.Variant?.Stock ?? item.Product!.Stock))
                throw new AppException($"Недостатньо товару «{item.Product!.Name}» на складі.", 409);

        var order = new Order
        {
            UserId = userId,
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            Phone = dto.Phone.Trim(),
            TotalPrice = cart.Items.Sum(i => (i.Product!.Price + (i.Variant?.PriceDelta ?? 0)) * i.Quantity),
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                ProductImageUrl = i.Product.Images.OrderByDescending(img => img.IsMain).ThenBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                Price = i.Product.Price + (i.Variant?.PriceDelta ?? 0),
                Quantity = i.Quantity
            }).ToList()
        };

        foreach (var item in cart.Items)
        {
            item.Product!.Stock -= item.Quantity;
            item.Product.UpdatedAt = DateTime.UtcNow;
            if (item.Variant is not null) item.Variant.Stock -= item.Quantity;
        }

        db.CartItems.RemoveRange(cart.Items);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return await LoadAsync(order.Id, ct);
    }

    public async Task<PagedResult<OrderDto>> GetMineAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
        await PageAsync(db.Orders.AsNoTracking().Where(o => o.UserId == userId), page, pageSize, ct);

    public async Task<OrderDto> GetByIdAsync(Guid userId, int orderId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new AppException("Замовлення не знайдено.", 404);

        if (!isAdmin && order.UserId != userId) throw new AppException("Немає доступу.", 403);

        return order.Adapt<OrderDto>();
    }

    public async Task<PagedResult<OrderDto>> GetAllAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (status is { } value) query = query.Where(o => o.Status == value);

        return await PageAsync(query, page, pageSize, ct);
    }

    public async Task<OrderDto> UpdateStatusAsync(int orderId, UpdateOrderStatusDto dto, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new AppException("Замовлення не знайдено.", 404);

        order.Status = dto.Status;
        await db.SaveChangesAsync(ct);

        return order.Adapt<OrderDto>();
    }

    private static async Task<PagedResult<OrderDto>> PageAsync(IQueryable<Order> query, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 10 : pageSize;

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<OrderDto>()
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(items, page, pageSize, totalCount);
    }

    private async Task<OrderDto> LoadAsync(int id, CancellationToken ct) =>
        await db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.Id == id).ProjectToType<OrderDto>().FirstAsync(ct);
}