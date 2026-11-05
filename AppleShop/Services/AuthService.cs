namespace AppleShop.Services;

using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Middleware;
using Microsoft.AspNetCore.Identity;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
}

public sealed class AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(dto.Email) is not null)
            throw new AppException("Користувач з таким email вже існує.", 409);

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            //Phone = dto.Phone,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) throw new AppException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, "Customer");
        return await BuildAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, dto.Password))
            throw new AppException("Невірний email або пароль.", 401);

        return await BuildAsync(user);
    }

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new AppException("Користувача не знайдено.", 404);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            //Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Roles = (await userManager.GetRolesAsync(user)).ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    private async Task<AuthResponseDto> BuildAsync(ApplicationUser user)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
               // Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Roles = roles,
                CreatedAt = user.CreatedAt
            }
        };
    }
}