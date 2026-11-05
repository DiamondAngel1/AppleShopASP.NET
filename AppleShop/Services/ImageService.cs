using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppleShop.Services;

using AppleShop.Middleware;
using AppleShop.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

public sealed class ImageService(
    IOptions<StorageOptions> options,
    IWebHostEnvironment environment,
    ILogger<ImageService> logger) : IImageService
{
    private const int MaxDimension = 1600;
    private const int ThumbnailSize = 400;
    private const int AvatarSize = 512;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly StorageOptions _storage = options.Value;

    public Task<StoredImage> SaveProductImageAsync(IFormFile file, CancellationToken ct = default) => SaveAsync(file, "products", false, ct);
    public Task<StoredImage> SaveCategoryImageAsync(IFormFile file, CancellationToken ct = default) => SaveAsync(file, "categories", false, ct);
    public async Task<string> SaveAvatarAsync(IFormFile file, CancellationToken ct = default) => (await SaveAsync(file, "avatars", true, ct)).Url;

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var relative = url.StartsWith(_storage.BaseUrl, StringComparison.OrdinalIgnoreCase)
                ? url[_storage.BaseUrl.Length..]
                : url;

            relative = relative.Split('?')[0];

            var root = GetRootPath();
            var full = Path.GetFullPath(Path.Combine(root, relative.TrimStart('/', '\\')));

            if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) return;
            if (File.Exists(full)) File.Delete(full);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to delete {Url}", url);
        }
    }

    private async Task<StoredImage> SaveAsync(IFormFile file, string folder, bool isAvatar, CancellationToken ct)
    {
        if (file.Length == 0) throw new AppException("Файл порожній.");
        if (file.Length > _storage.MaxFileSizeBytes) throw new AppException($"Файл перевищує {_storage.MaxFileSizeMb} МБ.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new AppException("Дозволені формати: jpg, jpeg, png, webp.");

        var directory = Path.Combine(GetRootPath(), folder);
        Directory.CreateDirectory(directory);

        var name = Guid.NewGuid().ToString("N");
        var url = $"{_storage.BaseUrl.TrimEnd('/')}/{folder}/{name}.webp";

        await using var upload = file.OpenReadStream();
        using var image = await Image.LoadAsync(upload, ct);

        if (isAvatar)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(AvatarSize, AvatarSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            await image.SaveAsWebpAsync(Path.Combine(directory, $"{name}.webp"), new WebpEncoder { Quality = 90 }, ct);
            return new StoredImage(url, url);
        }

        using var main = image.Clone(x => x.Resize(new ResizeOptions { Size = new Size(MaxDimension, MaxDimension), Mode = ResizeMode.Max }));
        using var thumb = image.Clone(x => x.Resize(new ResizeOptions { Size = new Size(ThumbnailSize, ThumbnailSize), Mode = ResizeMode.Max }));

        await main.SaveAsWebpAsync(Path.Combine(directory, $"{name}.webp"), new WebpEncoder { Quality = 88 }, ct);
        await thumb.SaveAsWebpAsync(Path.Combine(directory, $"{name}_thumb.webp"), new WebpEncoder { Quality = 82 }, ct);

        return new StoredImage(url, $"{_storage.BaseUrl.TrimEnd('/')}/{folder}/{name}_thumb.webp");
    }

    private string GetRootPath()
    {
        var root = _storage.RootPath;
        if (Path.IsPathRooted(root)) return root;

        var contentRoot = environment.ContentRootPath;
        var webRoot = environment.WebRootPath ?? Path.Combine(contentRoot, "wwwroot");

        return root.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(contentRoot, root)
            : Path.Combine(webRoot, root);
    }
}