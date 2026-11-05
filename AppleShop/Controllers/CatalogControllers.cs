namespace AppleShop.Controllers;

using AppleShop.DTOs;
using AppleShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    /// <summary>Каталог з пошуком, фільтрами, сортуванням і пагінацією.</summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "Catalog")]
    [ProducesResponseType(typeof(PagedResult<ProductCardDto>), 200)]
    public async Task<ActionResult<PagedResult<ProductCardDto>>> Search([FromQuery] ProductQuery query, CancellationToken ct)
        => Ok(await products.SearchAsync(query, ct));

    /// <summary>Рекомендовані товари.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "Catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductCardDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<ProductCardDto>>> Featured([FromQuery] int take, CancellationToken ct)
        => Ok(await products.GetFeaturedAsync(take, ct));

    /// <summary>Новинки для банера.</summary>
    [HttpGet("new-arrivals")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "Catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductCardDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<ProductCardDto>>> NewArrivals([FromQuery] int take, CancellationToken ct)
        => Ok(await products.GetNewArrivalsAsync(take, ct));

    /// <summary>Деталі товару за slug.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "Catalog")]
    [ProducesResponseType(typeof(ProductDetailsDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<ProductDetailsDto>> GetBySlug(string slug, CancellationToken ct)
        => Ok(await products.GetBySlugAsync(slug, ct));

    /// <summary>Створення товару з фото (multipart). Specs і Variants — JSON-рядки.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductDetailsDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<ActionResult<ProductDetailsDto>> Create([FromForm] CreateProductDto dto, CancellationToken ct)
    {
        var created = await products.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    /// <summary>Оновлення товару.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDetailsDto), 200)]
    public async Task<ActionResult<ProductDetailsDto>> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
        => Ok(await products.UpdateAsync(id, dto, ct));

    /// <summary>Додавання одного або кількох фото (multipart).</summary>
    [HttpPost("{id:int}/images")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<ProductImageDto>>> AddImages(int id, [FromForm] UploadImagesDto dto, CancellationToken ct)
        => Ok(await products.AddImagesAsync(id, dto.Files, ct));

    /// <summary>Видалення фото (файли видаляються з диска).</summary>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        await products.DeleteImageAsync(id, imageId, ct);
        return NoContent();
    }

    /// <summary>Зробити фото головним.</summary>
    [HttpPut("{id:int}/images/{imageId:int}/main")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SetMain(int id, int imageId, CancellationToken ct)
    {
        await products.SetMainImageAsync(id, imageId, ct);
        return NoContent();
    }

    /// <summary>Зміна порядку фото.</summary>
    [HttpPut("{id:int}/images/reorder")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<ProductImageDto>>> Reorder(int id, [FromBody] ReorderImagesDto dto, CancellationToken ct)
        => Ok(await products.ReorderImagesAsync(id, dto, ct));

    /// <summary>Видалення товару.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController(ICategoryService categories) : ControllerBase
{
    /// <summary>Усі категорії з ImageUrl.</summary>
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "Catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await categories.GetAllAsync(ct));

    /// <summary>Категорія за slug.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug, CancellationToken ct)
        => Ok(await categories.GetBySlugAsync(slug, ct));

    /// <summary>Створення категорії із зображенням (multipart).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    public async Task<ActionResult<CategoryDto>> Create([FromForm] CategoryUpsertFormDto dto, CancellationToken ct)
    {
        var created = await categories.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    /// <summary>Редагування категорії.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromForm] CategoryUpsertFormDto dto, CancellationToken ct)
        => Ok(await categories.UpdateAsync(id, dto, ct));

    /// <summary>Видалення категорії без товарів.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/products/{productId:int}/reviews")]
[Produces("application/json")]
public sealed class ReviewsController(IReviewService reviews, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Відгуки товару (від новіших).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), 200)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetForProduct(int productId, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
        => Ok(await reviews.GetForProductAsync(productId, page, pageSize, ct));

    /// <summary>Залишити відгук (один на товар від користувача).</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<ReviewDto>> Create(int productId, [FromBody] CreateReviewDto dto, CancellationToken ct)
        => StatusCode(201, await reviews.CreateAsync(productId, currentUser.RequireUserId(), dto, ct));
}