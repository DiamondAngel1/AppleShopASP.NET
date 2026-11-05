using Microsoft.AspNetCore.Http;

namespace AppleShop.DTOs;

using AppleShop.Entities;

public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items; Page = page; PageSize = pageSize; TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public sealed class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = [];
    public int OrdersCount { get; set; }
    public int FavoritesCount { get; set; }
}

public sealed class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public sealed class UploadFileDto { public IFormFile File { get; set; } = default!; }
public sealed class UploadImagesDto { public List<IFormFile> Files { get; set; } = []; }

public sealed class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int ProductsCount { get; set; }
}

public sealed class CategoryUpsertFormDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public IFormFile? Image { get; set; }
}

public sealed class ProductImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ProductVariantDto
{
    public int Id { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public int Stock { get; set; }
}

public sealed class ProductSpecDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class ProductCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int Stock { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string> Colors { get; set; } = [];
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public int? DiscountPercent => OldPrice is > 0 && OldPrice > Price ? (int)Math.Round((1 - Price / OldPrice.Value) * 100) : null;
}

public sealed class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int Stock { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    public List<ProductImageDto> Images { get; set; } = [];
    public List<ProductVariantDto> Variants { get; set; } = [];
    public List<ProductSpecDto> Specs { get; set; } = [];
}

public sealed class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    /// <summary>JSON: [{"key":"Display","value":"6.1"}]</summary>
    public string? Specs { get; set; }
    /// <summary>JSON: [{"color":"Black","storage":"256GB","priceDelta":100,"stock":5}]</summary>
    public string? Variants { get; set; }
    public List<IFormFile>? Images { get; set; }
}

public sealed class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsNew { get; set; }
    public string? Specs { get; set; }
    public string? Variants { get; set; }
}

public sealed class ReorderImagesDto { public List<int> ImageIds { get; set; } = []; }

public sealed class ProductQuery
{
    private int _page = 1;
    private int _pageSize = 12;

    public string? Search { get; set; }
    public string? Category { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Color { get; set; }
    public bool? InStockOnly { get; set; }
    public string? Sort { get; set; }

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; set => _pageSize = value is < 1 or > 60 ? 12 : value; }
}

public sealed class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int? VariantId { get; set; }
    public string? Color { get; set; }
    public string? Storage { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class CartDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = [];
    public decimal TotalPrice { get; set; }
    public int TotalQuantity { get; set; }
}

public sealed class AddCartItemDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartItemDto { public int Quantity { get; set; } }

public sealed class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public sealed class OrderDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public sealed class CreateOrderDto
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public sealed class UpdateOrderStatusDto { public OrderStatus Status { get; set; } }

public sealed class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}