using Microsoft.AspNetCore.Http;

namespace AppleShop.Services;

using AppleShop.Data;
using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public interface IProfileService
{
    Task<ProfileDto> GetAsync(Guid userId, CancellationToken ct = default);
    Task<ProfileDto> UpdateAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task<string> SetAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default);
    Task RemoveAvatarAsync(Guid userId, CancellationToken ct = default);
}

public sealed class ProfileService(UserManager<ApplicationUser> userManager, AppDbContext db, IImageService images) : IProfileService
{
    public async Task<ProfileDto> GetAsync(Guid userId, CancellationToken ct = default) =>
        await MapAsync(await GetUserAsync(userId), ct);

    public async Task<ProfileDto> UpdateAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId);

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        //user.Phone = dto.Phone?.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new AppException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return await MapAsync(user, ct);
    }

    public async Task<string> SetAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId);
        var previous = user.AvatarUrl;
        var url = await images.SaveAvatarAsync(file, ct);

        user.AvatarUrl = url;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            images.Delete(url);
            throw new AppException("Не вдалося зберегти аватар.");
        }

        images.Delete(previous);
        return url;
    }

    public async Task RemoveAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUserAsync(userId);
        if (string.IsNullOrWhiteSpace(user.AvatarUrl)) return;

        var previous = user.AvatarUrl;
        user.AvatarUrl = null;

        if (!(await userManager.UpdateAsync(user)).Succeeded)
            throw new AppException("Не вдалося видалити аватар.");

        images.Delete(previous);
    }

    private async Task<ApplicationUser> GetUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new AppException("Користувача не знайдено.", 404);

    private async Task<ProfileDto> MapAsync(ApplicationUser user, CancellationToken ct) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        //Phone = user.Phone,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = user.CreatedAt,
        Roles = (await userManager.GetRolesAsync(user)).ToList(),
        OrdersCount = await db.Orders.CountAsync(o => o.UserId == user.Id, ct),
        FavoritesCount = await db.Favorites.CountAsync(f => f.UserId == user.Id, ct)
    };
}