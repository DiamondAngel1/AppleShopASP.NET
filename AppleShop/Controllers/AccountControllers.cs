namespace AppleShop.Controllers;

using AppleShop.DTOs;
using AppleShop.Entities;
using AppleShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Реєстрація покупця.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
        => Ok(await auth.RegisterAsync(dto, ct));

    /// <summary>Вхід за email і паролем.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
        => Ok(await auth.LoginAsync(dto, ct));

    /// <summary>Поточний користувач разом з AvatarUrl.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await auth.GetMeAsync(currentUser.RequireUserId(), ct));
}

[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public sealed class ProfileController(IProfileService profile, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Профіль користувача.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), 200)]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct)
        => Ok(await profile.GetAsync(currentUser.RequireUserId(), ct));

    /// <summary>Оновлення імені, прізвища, телефону.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileDto dto, CancellationToken ct)
        => Ok(await profile.UpdateAsync(currentUser.RequireUserId(), dto, ct));

    /// <summary>Завантаження або заміна аватара (multipart, поле "file").</summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadFileDto dto, CancellationToken ct)
        => Ok(new { avatarUrl = await profile.SetAvatarAsync(currentUser.RequireUserId(), dto.File, ct) });

    /// <summary>Видалення аватара.</summary>
    [HttpDelete("avatar")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        await profile.RemoveAvatarAsync(currentUser.RequireUserId(), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/cart")]
[Authorize]
[Produces("application/json")]
public sealed class CartController(ICartService cart, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Поточний кошик.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), 200)]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct)
        => Ok(await cart.GetAsync(currentUser.RequireUserId(), ct));

    /// <summary>Додати товар (за потреби з варіантом).</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<CartDto>> Add([FromBody] AddCartItemDto dto, CancellationToken ct)
        => Ok(await cart.AddItemAsync(currentUser.RequireUserId(), dto, ct));

    /// <summary>Змінити кількість позиції (0 — видалити).</summary>
    [HttpPut("items/{id:int}")]
    [ProducesResponseType(typeof(CartDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<CartDto>> Update(int id, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
        => Ok(await cart.UpdateItemAsync(currentUser.RequireUserId(), id, dto, ct));

    /// <summary>Видалити позицію.</summary>
    [HttpDelete("items/{id:int}")]
    [ProducesResponseType(typeof(CartDto), 200)]
    public async Task<ActionResult<CartDto>> Remove(int id, CancellationToken ct)
        => Ok(await cart.RemoveItemAsync(currentUser.RequireUserId(), id, ct));
}

[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orders, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Оформити замовлення з кошика.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
        => StatusCode(201, await orders.CreateFromCartAsync(currentUser.RequireUserId(), dto, ct));

    /// <summary>Мої замовлення.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
    public async Task<ActionResult<PagedResult<OrderDto>>> Mine([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
        => Ok(await orders.GetMineAsync(currentUser.RequireUserId(), page, pageSize, ct));

    /// <summary>Усі замовлення (Admin).</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<PagedResult<OrderDto>>> All([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] OrderStatus? status, CancellationToken ct)
        => Ok(await orders.GetAllAsync(page, pageSize, status, ct));

    /// <summary>Замовлення за Id (власник або Admin).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<OrderDto>> ById(int id, CancellationToken ct)
        => Ok(await orders.GetByIdAsync(currentUser.RequireUserId(), id, currentUser.IsAdmin, ct));

    /// <summary>Зміна статусу (Admin).</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    public async Task<ActionResult<OrderDto>> Status(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
        => Ok(await orders.UpdateStatusAsync(id, dto, ct));
}

[ApiController]
[Route("api/favorites")]
[Authorize]
[Produces("application/json")]
public sealed class FavoritesController(IFavoriteService favorites, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Обрані товари.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductCardDto>), 200)]
    public async Task<ActionResult<PagedResult<ProductCardDto>>> Get([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
        => Ok(await favorites.GetAsync(currentUser.RequireUserId(), page, pageSize, ct));

    /// <summary>Додати товар в обране.</summary>
    [HttpPost("{productId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Add(int productId, CancellationToken ct)
    {
        await favorites.AddAsync(currentUser.RequireUserId(), productId, ct);
        return NoContent();
    }

    /// <summary>Прибрати товар з обраного.</summary>
    [HttpDelete("{productId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await favorites.RemoveAsync(currentUser.RequireUserId(), productId, ct);
        return NoContent();
    }
}