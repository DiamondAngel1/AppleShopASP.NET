using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AppleShop.Data;

using AppleShop.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class DbSeeder
{
    private static readonly Color[] Palette =
    [
        Color.FromPixel(new Rgb24(198, 244, 50)),
        Color.FromPixel(new Rgb24(19, 65, 50)),
        Color.FromPixel(new Rgb24(13, 51, 38)),
        Color.FromPixel(new Rgb24(7, 32, 26)),
        Color.FromPixel(new Rgb24(80, 200, 160)),
        Color.FromPixel(new Rgb24(240, 240, 235)),
        Color.FromPixel(new Rgb24(120, 140, 130))
    ];

    private sealed record Seed(
        string Name, string Slug, string Category, string Short, string Description,
        decimal Price, decimal? Old, int Stock, bool Featured, bool New,
        (string Color, string Storage, decimal Delta, int Stock)[] Variants,
        (string Key, string Value)[] Specs);

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));

        await EnsureUserAsync(users, "admin@applestore.com", "Admin123!", "Store", "Admin", "Admin");
        await EnsureUserAsync(users, "customer@applestore.com", "Customer123!", "Demo", "Customer", "Customer");

        if (await db.Categories.AnyAsync()) return;

        var root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        foreach (var folder in new[] { "products", "categories", "avatars" })
            Directory.CreateDirectory(Path.Combine(root, folder));

        var categories = new List<Category>
        {
            new() { Name = "iPhone", Slug = "iphone" },
            new() { Name = "Mac", Slug = "mac" },
            new() { Name = "iPad", Slug = "ipad" },
            new() { Name = "Apple Watch", Slug = "apple-watch" },
            new() { Name = "AirPods", Slug = "airpods" },
            new() { Name = "Accessories", Slug = "accessories" }
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        for (var i = 0; i < categories.Count; i++)
            categories[i].ImageUrl = WriteImage(root, "categories", categories[i].Slug, Palette[i % Palette.Length], Palette[(i + 3) % Palette.Length]);

        await db.SaveChangesAsync();

        var bySlug = categories.ToDictionary(c => c.Slug, c => c.Id);
        var now = DateTime.UtcNow;

        for (var i = 0; i < Catalog.Length; i++)
        {
            var seed = Catalog[i];

            var product = new Product
            {
                Name = seed.Name,
                Slug = seed.Slug,
                CategoryId = bySlug[seed.Category],
                ShortDescription = seed.Short,
                Description = seed.Description,
                Price = seed.Price,
                OldPrice = seed.Old,
                Stock = seed.Stock,
                IsFeatured = seed.Featured,
                IsNew = seed.New,
                CreatedAt = now.AddDays(-i),
                UpdatedAt = now.AddDays(-i)
            };

            foreach (var variant in seed.Variants)
                product.Variants.Add(new ProductVariant
                {
                    Color = variant.Color,
                    Storage = variant.Storage,
                    PriceDelta = variant.Delta,
                    Stock = variant.Stock
                });

            foreach (var spec in seed.Specs)
                product.Specs.Add(new ProductSpec { Key = spec.Key, Value = spec.Value });

            var main = WriteImage(root, "products", seed.Slug, Palette[i % Palette.Length], Palette[(i + 4) % Palette.Length]);
            var alt = WriteImage(root, "products", seed.Slug + "-alt", Palette[(i + 4) % Palette.Length], Palette[i % Palette.Length]);

            product.Images.Add(new ProductImage { Url = main, ThumbnailUrl = main.Replace(".webp", "_thumb.webp"), IsMain = true, SortOrder = 0 });
            product.Images.Add(new ProductImage { Url = alt, ThumbnailUrl = alt.Replace(".webp", "_thumb.webp"), SortOrder = 1 });

            db.Products.Add(product);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, string email, string password, string first, string last, string role)
    {
        if (await users.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = first,
            LastName = last
        };

        if ((await users.CreateAsync(user, password)).Succeeded)
            await users.AddToRoleAsync(user, role);
    }

    private static string WriteImage(string root, string folder, string slug, Color from, Color to)
    {
        using var image = CreateGradient(1200, from, to);

        image.SaveAsWebp(Path.Combine(root, folder, slug + ".webp"), new WebpEncoder { Quality = 86 });

        using var thumb = image.Clone(x => x.Resize(400, 400));
        thumb.SaveAsWebp(Path.Combine(root, folder, slug + "_thumb.webp"), new WebpEncoder { Quality = 80 });

        return "/uploads/" + folder + "/" + slug + ".webp";
    }

    private static Image<Rgba32> CreateGradient(int size, Color from, Color to)
    {
        var image = new Image<Rgba32>(size, size);
        var start = from.ToPixel<Rgba32>();
        var end = to.ToPixel<Rgba32>();

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < row.Length; x++)
                {
                    var t = (x + y) / (float)(accessor.Width + accessor.Height - 2);
                    var dx = x - accessor.Width * 0.62f;
                    var dy = y - accessor.Height * 0.34f;
                    var glow = MathF.Max(0f, 1f - MathF.Sqrt(dx * dx + dy * dy) / (accessor.Width * 0.55f)) * 0.45f;

                    row[x] = new Rgba32(
                        (byte)Math.Clamp(start.R + (end.R - start.R) * t + 255 * glow, 0, 255),
                        (byte)Math.Clamp(start.G + (end.G - start.G) * t + 255 * glow, 0, 255),
                        (byte)Math.Clamp(start.B + (end.B - start.B) * t + 255 * glow, 0, 255));
                }
            }
        });

        return image;
    }

    private static readonly Seed[] Catalog =
    [
        new("iPhone 16 Pro Max", "iphone-16-pro-max", "iphone",
            "Титановий корпус, чип A18 Pro і камера 48 Мп.",
            "## iPhone 16 Pro Max\n\nНайпотужніший iPhone з **титановим** корпусом.\n\n- Дисплей 6.9 Super Retina XDR\n- Камера 48 Мп з 5x зумом\n- До 33 годин відео\n\n> Максимум можливостей.",
            1499m, 1649m, 24, true, true,
            [("Natural Titanium", "256GB", 0m, 8), ("Black Titanium", "512GB", 200m, 6), ("Desert Titanium", "1TB", 400m, 4)],
            [("Display", "6.9 Super Retina XDR"), ("Chip", "A18 Pro"), ("Camera", "48 Мп + 48 Мп + 12 Мп")]),

        new("iPhone 16 Pro", "iphone-16-pro", "iphone",
            "Компактний флагман з A18 Pro та 120 Гц.",
            "## iPhone 16 Pro\n\nУсі можливості Pro Max у форматі 6.3.\n\n- Чип A18 Pro\n- Camera Control\n- 4K120 Dolby Vision",
            1199m, 1299m, 30, true, true,
            [("Natural Titanium", "128GB", 0m, 10), ("Blue Titanium", "256GB", 120m, 12), ("White Titanium", "512GB", 380m, 8)],
            [("Display", "6.3 Super Retina XDR"), ("Chip", "A18 Pro"), ("Water", "IP68")]),

        new("iPhone 16", "iphone-16", "iphone",
            "Новий дизайн, A18 і Camera Control.",
            "## iPhone 16\n\nЯскравий і швидкий iPhone.\n\n- Дисплей 6.1\n- Подвійна камера 48 Мп\n- USB-C та MagSafe",
            899m, null, 42, true, true,
            [("Ultramarine", "128GB", 0m, 15), ("Pink", "256GB", 100m, 14), ("Teal", "512GB", 320m, 13)],
            [("Display", "6.1 Super Retina XDR"), ("Chip", "A18")]),

        new("iPhone 15", "iphone-15", "iphone",
            "Dynamic Island та USB-C.",
            "## iPhone 15\n\nПеревірена класика.\n\n- Dynamic Island\n- Камера 48 Мп\n- USB-C",
            699m, 799m, 55, false, false,
            [("Black", "128GB", 0m, 20), ("Blue", "256GB", 90m, 18), ("Green", "512GB", 280m, 17)],
            [("Display", "6.1 Super Retina XDR"), ("Chip", "A16 Bionic")]),

        new("iPhone SE", "iphone-se", "iphone",
            "Компактний iPhone з Touch ID.",
            "## iPhone SE\n\nНайдоступніший iPhone.\n\n- 4.7 Retina HD\n- Touch ID\n- A15 Bionic",
            429m, null, 18, false, false,
            [("Midnight", "64GB", 0m, 9), ("Starlight", "128GB", 60m, 9)],
            [("Display", "4.7 Retina HD"), ("Chip", "A15 Bionic")]),

        new("MacBook Pro 14 M4", "macbook-pro-14-m4", "mac",
            "Чип M4 Pro, Liquid Retina XDR, 18 годин.",
            "## MacBook Pro 14\n\nДля найважчих задач.\n\n- M4 Pro 12-core\n- 120 Гц XDR\n- До 18 годин",
            2199m, 2399m, 12, true, true,
            [("Space Black", "512GB SSD", 0m, 6), ("Silver", "1TB SSD", 250m, 4), ("Space Black", "2TB SSD", 600m, 2)],
            [("Chip", "Apple M4 Pro"), ("Display", "14.2 Liquid Retina XDR"), ("Memory", "24 ГБ")]),

        new("MacBook Air 13 M3", "macbook-air-13-m3", "mac",
            "Тонкий, безшумний і легкий.",
            "## MacBook Air 13 M3\n\n1.24 кг продуктивності.\n\n- Чип M3\n- 13.6 Liquid Retina\n- Пасивне охолодження",
            1099m, 1199m, 26, true, false,
            [("Midnight", "256GB SSD", 0m, 12), ("Starlight", "512GB SSD", 200m, 8), ("Sky Blue", "1TB SSD", 420m, 6)],
            [("Chip", "Apple M3"), ("Weight", "1.24 кг")]),

        new("Mac mini M4", "mac-mini-m4", "mac",
            "Настільний Mac у мінімальному корпусі.",
            "## Mac mini M4\n\nПотужність у 12.7 см.\n\n- Чип M4\n- 16 ГБ памʼяті\n- Thunderbolt 4",
            649m, null, 20, false, true,
            [("Silver", "256GB SSD", 0m, 12), ("Silver", "512GB SSD", 180m, 8)],
            [("Chip", "Apple M4"), ("Ports", "3x Thunderbolt 4")]),

        new("iMac 24 M4", "imac-24-m4", "mac",
            "4.5K дисплей і чип M4.",
            "## iMac 24\n\nНайяскравіший моноблок.\n\n- 4.5K Retina\n- Чип M4\n- 7 кольорів",
            1399m, 1499m, 9, false, true,
            [("Blue", "256GB SSD", 0m, 5), ("Green", "512GB SSD", 220m, 4)],
            [("Display", "24 4.5K Retina"), ("Chip", "Apple M4")]),

        new("Mac Studio M2 Max", "mac-studio-m2-max", "mac",
            "Студійна продуктивність без компромісів.",
            "## Mac Studio\n\nДля 8K і 3D.\n\n- M2 Max\n- 32 ГБ\n- 6x Thunderbolt 4",
            2499m, null, 6, false, false,
            [("Silver", "1TB SSD", 0m, 4), ("Silver", "2TB SSD", 500m, 2)],
            [("Chip", "Apple M2 Max"), ("Memory", "32 ГБ")]),

        new("iPad Pro 13 M4", "ipad-pro-13-m4", "ipad",
            "Найтонший iPad з тандемним OLED.",
            "## iPad Pro 13\n\n5.1 мм і Ultra Retina XDR.\n\n- Чип M4\n- Tandem OLED\n- Apple Pencil Pro",
            1299m, 1399m, 15, true, true,
            [("Space Black", "256GB", 0m, 7), ("Silver", "512GB", 300m, 5), ("Space Black", "1TB", 700m, 3)],
            [("Display", "13 Ultra Retina XDR"), ("Chip", "Apple M4")]),

        new("iPad Air 11 M2", "ipad-air-11-m2", "ipad",
            "Легкий планшет для роботи й творчості.",
            "## iPad Air 11\n\nБаланс ціни і продуктивності.\n\n- Чип M2\n- 11 Liquid Retina",
            699m, 749m, 22, true, false,
            [("Blue", "128GB", 0m, 10), ("Purple", "256GB", 120m, 7), ("Starlight", "512GB", 380m, 5)],
            [("Display", "11 Liquid Retina"), ("Chip", "Apple M2")]),

        new("iPad mini 7", "ipad-mini-7", "ipad",
            "Компактний iPad, що вміщується в долоню.",
            "## iPad mini 7\n\n8.3 дюйма зручності.\n\n- A17 Pro\n- Apple Pencil Pro",
            499m, null, 19, false, true,
            [("Space Gray", "128GB", 0m, 10), ("Blue", "256GB", 110m, 9)],
            [("Display", "8.3 Liquid Retina"), ("Chip", "A17 Pro")]),

        new("iPad 10", "ipad-10", "ipad",
            "Класичний iPad для щоденних задач.",
            "## iPad 10\n\nНайдоступніший планшет.\n\n- 10.9 дюйма\n- A14 Bionic",
            349m, 399m, 34, false, false,
            [("Blue", "64GB", 0m, 18), ("Pink", "256GB", 130m, 16)],
            [("Display", "10.9 Liquid Retina"), ("Chip", "A14 Bionic")]),

        new("Apple Watch Ultra 2", "apple-watch-ultra-2", "apple-watch",
            "Титан, 3000 ніт, дайвінг до 100 м.",
            "## Apple Watch Ultra 2\n\nДля тих, хто не зупиняється.\n\n- 49 мм титан\n- 3000 ніт\n- До 72 годин",
            799m, 849m, 14, true, false,
            [("Titanium", "49mm", 0m, 8), ("Black Titanium", "49mm", 60m, 6)],
            [("Case", "49 мм Titanium"), ("Display", "3000 ніт")]),

        new("Apple Watch Series 10", "apple-watch-series-10", "apple-watch",
            "Тонший корпус і більший дисплей.",
            "## Watch Series 10\n\nНайтонший Apple Watch.\n\n- 46 мм\n- Always-On Retina\n- Датчик температури",
            429m, 479m, 28, true, true,
            [("Jet Black", "42mm", 0m, 10), ("Rose Gold", "46mm", 40m, 10), ("Silver", "46mm", 40m, 8)],
            [("Case", "46 мм"), ("Chip", "S10 SiP")]),

        new("Apple Watch SE", "apple-watch-se", "apple-watch",
            "Все важливе за доступною ціною.",
            "## Watch SE\n\nФітнес і безпека.\n\n- 40/44 мм\n- Виявлення аварій",
            249m, null, 33, false, false,
            [("Midnight", "40mm", 0m, 17), ("Starlight", "44mm", 30m, 16)],
            [("Case", "44 мм"), ("Chip", "S8 SiP")]),

        new("AirPods Pro 2", "airpods-pro-2", "airpods",
            "Активне шумозаглушення нового рівня.",
            "## AirPods Pro 2\n\nUSB-C, Adaptive Audio.\n\n- ANC у 2 рази ефективніше\n- До 6 годин",
            249m, 279m, 48, true, false,
            [("White", "USB-C", 0m, 26), ("White", "MagSafe", 0m, 22)],
            [("Chip", "H2"), ("ANC", "Adaptive Audio")]),

        new("AirPods 4", "airpods-4", "airpods",
            "Відкритий дизайн з підтримкою ANC.",
            "## AirPods 4\n\nКомфорт без амбушурів.\n\n- Чип H2\n- Голосова ізоляція",
            179m, null, 52, true, true,
            [("White", "Standard", 0m, 30), ("White", "ANC", 40m, 22)],
            [("Chip", "H2"), ("Case", "USB-C")]),

        new("AirPods Max", "airpods-max", "airpods",
            "Преміальні накладні навушники.",
            "## AirPods Max\n\nАлюміній і Hi-Fi звук.\n\n- 40-мм драйвери\n- До 20 годин",
            549m, 599m, 11, false, false,
            [("Space Gray", "USB-C", 0m, 6), ("Blue", "USB-C", 0m, 5)],
            [("Driver", "40 мм"), ("Battery", "20 годин")]),

        new("Apple Pencil Pro", "apple-pencil-pro", "accessories",
            "Стилус з тактильним відгуком.",
            "## Apple Pencil Pro\n\nТочність до пікселя.\n\n- Barrel Roll\n- Find My",
            129m, null, 40, false, false,
            [("White", "Standard", 0m, 40)],
            [("Compatibility", "iPad Pro M4, iPad Air M2")]),

        new("MagSafe Charger", "magsafe-charger", "accessories",
            "Швидка бездротова зарядка 25 Вт.",
            "## MagSafe Charger\n\nЗручність без кабелю.\n\n- 25 Вт\n- Кабель 1 м",
            49m, 59m, 70, false, false,
            [("White", "1m", 0m, 40), ("White", "2m", 15m, 30)],
            [("Power", "25 Вт"), ("Length", "1 м")]),

        new("Magic Keyboard", "magic-keyboard", "accessories",
            "Бездротова клавіатура з Touch ID.",
            "## Magic Keyboard\n\nДрук із задоволенням.\n\n- Touch ID\n- Bluetooth 5.1",
            149m, 169m, 25, false, false,
            [("White", "USB-C", 0m, 13), ("Black", "USB-C", 0m, 12)],
            [("Connection", "Bluetooth 5.1"), ("Battery", "1 місяць")])
    ];
}