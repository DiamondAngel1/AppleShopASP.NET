namespace AppleShop.Validators;

using System.Text.Json;
using AppleShop.DTOs;
using FluentValidation;

public sealed class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60);
    }
}

public sealed class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.OldPrice).GreaterThan(x => x.Price).When(x => x.OldPrice.HasValue);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Specs).Must(JsonRules.ValidSpecs);
        RuleFor(x => x.Variants).Must(JsonRules.ValidVariants);
    }
}

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(400);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

public sealed class ReorderImagesValidator : AbstractValidator<ReorderImagesDto>
{
    public ReorderImagesValidator()
    {
        RuleFor(x => x.ImageIds).NotEmpty();
        RuleFor(x => x.ImageIds).Must(ids => ids.Distinct().Count() == ids.Count);
    }
}

//public sealed class UploadImagesValidator : AbstractValidator<UploadImagesDto>
//{
  //  public UploadImagesValidator() => RuleFor(x => x.Files).NotEmpty();
//}

public sealed class AddCartItemValidator : AbstractValidator<AddCartItemDto>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
    }
}

public sealed class UpdateCartItemValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemValidator() => RuleFor(x => x.Quantity).InclusiveBetween(0, 99);
}

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
    }
}

public sealed class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1500);
    }
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60);
    }
}

public sealed class CategoryUpsertValidator : AbstractValidator<CategoryUpsertFormDto>
{
    public CategoryUpsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Slug).MaximumLength(80);
    }
}

internal static class JsonRules
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static bool ValidSpecs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            var items = JsonSerializer.Deserialize<List<Spec>>(json, Options);
            return items is not null && items.All(i => !string.IsNullOrWhiteSpace(i.Key));
        }
        catch (JsonException) { return false; }
    }

    public static bool ValidVariants(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            var items = JsonSerializer.Deserialize<List<Variant>>(json, Options);
            return items is not null && items.All(i => !string.IsNullOrWhiteSpace(i.Color) && i.Stock >= 0);
        }
        catch (JsonException) { return false; }
    }

    private sealed class Spec { public string Key { get; set; } = string.Empty; public string? Value { get; set; } }
    private sealed class Variant { public string Color { get; set; } = string.Empty; public int Stock { get; set; } }
}