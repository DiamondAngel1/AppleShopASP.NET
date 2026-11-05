namespace AppleShop.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AppleStore";
    public string Audience { get; set; } = "AppleStoreClient";
    public int ExpiresInMinutes { get; set; } = 1440;
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string RootPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";
    public int MaxFileSizeMb { get; set; } = 5;
    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}