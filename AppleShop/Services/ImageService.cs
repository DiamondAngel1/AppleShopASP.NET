using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppleShop.Middleware;
using AppleShop.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace AppleShop.Services;

public sealed class ImageService : IImageService
{
    private const int MaxDimension = 1600;
    private const int ThumbnailSize = 400;
    private const int AvatarSize = 512;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly StorageOptions _storage;
    private readonly ILogger<ImageService> _logger;
    private readonly Cloudinary _cloudinary;

    public ImageService(
        IOptions<StorageOptions> options,
        ILogger<ImageService> logger)
    {
        _storage = options.Value;
        _logger = logger;

        // Зчитуємо URL підключення з Environment Variables
        var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            throw new InvalidOperationException("Не задано змінну оточення CLOUDINARY_URL.");
        }

        // Ініціалізуємо клієнт Cloudinary
        _cloudinary = new Cloudinary(cloudinaryUrl);
        _cloudinary.Api.Secure = true;
    }

    public Task<StoredImage> SaveProductImageAsync(IFormFile file, CancellationToken ct = default) => SaveAsync(file, "products", false, ct);
    public Task<StoredImage> SaveCategoryImageAsync(IFormFile file, CancellationToken ct = default) => SaveAsync(file, "categories", false, ct);
    public async Task<string> SaveAvatarAsync(IFormFile file, CancellationToken ct = default) => (await SaveAsync(file, "avatars", true, ct)).Url;

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            // Витягуємо Public ID файлу з URL Cloudinary для його видалення
            // Стрічка виду: https://cloudinary.com
            var uri = new Uri(url);
            var segments = uri.Segments;

            // Нам потрібні останні сегменти (папка + ім'я файлу без розширення)
            if (segments.Length >= 2)
            {
                var folder = segments[^2].TrimEnd('/');
                var fileNameWithExtension = segments[^1];
                var publicId = $"{folder}/{Path.GetFileNameWithoutExtension(fileNameWithExtension)}";

                var deleteParams = new DeletionParams(publicId);
                _cloudinary.Destroy(deleteParams);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete from Cloudinary: {Url}", url);
        }
    }

    private async Task<StoredImage> SaveAsync(IFormFile file, string folder, bool isAvatar, CancellationToken ct)
    {
        if (file.Length == 0) throw new AppException("Файл порожній.");
        if (file.Length > _storage.MaxFileSizeBytes) throw new AppException($"Файл перевищує {_storage.MaxFileSizeMb} МБ.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new AppException("Дозволені формати: jpg, jpeg, png, webp.");

        var name = Guid.NewGuid().ToString("N");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(name, stream),
            PublicId = $"{folder}/{name}", // Файли будуть гарно розкладені по папках у хмарі
            Format = "webp" // Примусово конвертуємо в оптимізований WebP формат
        };

        if (isAvatar)
        {
            // Для аватарів робимо «розумне» обрізання по центру за допомогою штучного інтелекту хмари
            uploadParams.Transformation = new Transformation()
                .Width(AvatarSize).Height(AvatarSize).Crop("fill").Gravity("face");

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);
            if (uploadResult.Error != null) throw new AppException($"Помилка завантаження аватарки: {uploadResult.Error.Message}");

            return new StoredImage(uploadResult.SecureUrl.ToString(), uploadResult.SecureUrl.ToString());
        }

        // Для звичайних картинок товарів обмежуємо максимальну роздільну здатність
        uploadParams.Transformation = new Transformation().Width(MaxDimension).Height(MaxDimension).Crop("limit");

        var mainResult = await _cloudinary.UploadAsync(uploadParams, ct);
        if (mainResult.Error != null) throw new AppException($"Помилка завантаження зображення: {mainResult.Error.Message}");

        var mainUrl = mainResult.SecureUrl.ToString();

        // Створюємо посилання на Thumbnail (прев'ю) за допомогою вбудованих трансформацій Cloudinary на льоту
        // Замість фізичного завантаження другого файлу, ми просто додаємо параметри зміни розміру в URL
        var thumbUrl = mainUrl.Replace($"/upload/", $"/upload/c_limit,h_{ThumbnailSize},w_{ThumbnailSize}/");

        return new StoredImage(mainUrl, thumbUrl);
    }
}
