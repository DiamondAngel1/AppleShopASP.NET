using Microsoft.AspNetCore.Http;

namespace AppleShop.Services;

public sealed record StoredImage(string Url, string ThumbnailUrl);

public interface IImageService
{
    Task<StoredImage> SaveProductImageAsync(IFormFile file, CancellationToken ct = default);
    Task<StoredImage> SaveCategoryImageAsync(IFormFile file, CancellationToken ct = default);
    Task<string> SaveAvatarAsync(IFormFile file, CancellationToken ct = default);
    void Delete(string? url);
}