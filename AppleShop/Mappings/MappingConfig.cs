namespace AppleShop.Mappings;

using AppleShop.DTOs;
using AppleShop.Entities;
using Mapster;

public static class MappingConfig
{
    public static void Register()
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.NewConfig<Product, ProductCardDto>()
            .Map(d => d.CategoryName, s => s.Category!.Name)
            .Map(d => d.CategorySlug, s => s.Category!.Slug)
            .Map(d => d.ImageUrl, s => s.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault())
            .Map(d => d.ThumbnailUrl, s => s.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.ThumbnailUrl).FirstOrDefault())
            .Map(d => d.Colors, s => s.Variants.Select(v => v.Color).Distinct().ToList())
            .Map(d => d.AverageRating, s => s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => (double)r.Rating), 1) : 0)
            .Map(d => d.ReviewsCount, s => s.Reviews.Count);

        config.NewConfig<Product, ProductDetailsDto>()
            .Map(d => d.CategoryName, s => s.Category!.Name)
            .Map(d => d.CategorySlug, s => s.Category!.Slug)
            .Map(d => d.Images, s => s.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder))
            .Map(d => d.Specs, s => s.Specs.OrderBy(x => x.Id))
            .Map(d => d.Variants, s => s.Variants.OrderBy(x => x.Id))
            .Map(d => d.AverageRating, s => s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => (double)r.Rating), 1) : 0)
            .Map(d => d.ReviewsCount, s => s.Reviews.Count);

        config.NewConfig<Category, CategoryDto>().Map(d => d.ProductsCount, s => s.Products.Count);

        config.NewConfig<CartItem, CartItemDto>()
            .Map(d => d.ProductName, s => s.Product!.Name)
            .Map(d => d.ProductSlug, s => s.Product!.Slug)
            .Map(d => d.ImageUrl, s => s.Product!.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault())
            .Map(d => d.Color, s => s.Variant != null ? s.Variant.Color : null)
            .Map(d => d.Storage, s => s.Variant != null ? s.Variant.Storage : null)
            .Map(d => d.UnitPrice, s => s.Product!.Price + (s.Variant != null ? s.Variant.PriceDelta : 0))
            .Map(d => d.AvailableStock, s => s.Variant != null ? s.Variant.Stock : s.Product!.Stock);

        config.NewConfig<Order, OrderDto>().Map(d => d.Status, s => s.Status.ToString());

        config.NewConfig<Review, ReviewDto>()
            .Map(d => d.UserName, s => (s.User!.FirstName + " " + s.User.LastName).Trim())
            .Map(d => d.UserAvatarUrl, s => s.User!.AvatarUrl);

        config.Compile();
    }
}