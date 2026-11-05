using Microsoft.AspNetCore.Http;

namespace AppleShop.Services;

using System.Security.Claims;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAdmin { get; }
    Guid RequireUserId();
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var raw = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") ?? false;

    public Guid RequireUserId() => UserId ?? throw new UnauthorizedAccessException("Користувача не автентифіковано.");
}