// See https://aka.ms/new-console-template for more information

// Файл: backend/Program.cs

using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AppleShop.Data;
using AppleShop.Entities;
using AppleShop.Mappings;
using AppleShop.Middleware;
using AppleShop.Options;
using AppleShop.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);




// Логування в консоль.
// Не потребує окремого пакета Serilog.Sinks.File.
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Конфігурація
var connectionString =
    builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задано ConnectionStrings:Default у appsettings.json.");
}

var jwtSection =
    builder.Configuration.GetSection(JwtOptions.SectionName);

var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Не задано секцію Jwt у appsettings.json.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key)
    || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key має містити щонайменше 32 байти.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || jwtOptions.ExpiresInMinutes <= 0)
{
    throw new InvalidOperationException(
        "Перевірте Jwt:Issuer, Jwt:Audience та Jwt:ExpiresInMinutes.");
}

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(
        options => Encoding.UTF8.GetByteCount(options.Key) >= 32,
        "Jwt:Key має містити щонайменше 32 байти.")
    .Validate(
        options => options.ExpiresInMinutes > 0,
        "Jwt:ExpiresInMinutes має бути більшим за нуль.")
    .ValidateOnStart();

builder.Services
    .AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.RootPath),
        "Storage:RootPath не може бути порожнім.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.BaseUrl),
        "Storage:BaseUrl не може бути порожнім.")
    .Validate(
        options => options.MaxFileSizeMb is > 0 and <= 5,
        "Storage:MaxFileSizeMb має бути від 1 до 5.")
    .ValidateOnStart();

var storageOptions = builder.Configuration
    .GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();

// Ліміт всього multipart-запиту.
// Кожен окремий файл перевіряє ImageService.
const long maxRequestBodySize = 60L * 1024 * 1024;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodySize;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});

// PostgreSQL / EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.SignIn.RequireConfirmedEmail = false;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT Bearer
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultForbidScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)),

                ValidAlgorithms =
                [
                    SecurityAlgorithms.HmacSha256
                ],

                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.FromSeconds(30)
            };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.Headers["WWW-Authenticate"] =
                    "Bearer";

                await Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail:
                        "Потрібна авторизація. Увійдіть у свій обліковий запис.",
                    instance: context.Request.Path.Value
                ).ExecuteAsync(context.HttpContext);
            },

            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                await Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail:
                        "Недостатньо прав для виконання цієї дії.",
                    instance: context.Request.Path.Value
                ).ExecuteAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization();

// CORS
var configuredOrigins = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>()?.AllowedOrigins;

var allowedOrigins = configuredOrigins is { Length: > 0 }
    ? configuredOrigins
    :
    [
        "http://localhost:5173",
        "http://localhost:3000"
    ];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Сервіси
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<
    AppleShop.Validators.RegisterValidator>();

builder.Services.AddScoped<ValidationFilter>();

// Контролери / JSON / ProblemDetails
builder.Services.AddProblemDetails();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Mapster
MappingConfig.Register();

// Кеш публічних GET
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Catalog", policy =>
    {
        policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("*")
            .Tag("catalog");
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AppleStore API",
        Version = "v1",
        Description =
            "API інтернет-магазину AppleStore. " +
            "Для захищених запитів використовуйте JWT-токен."
    });

    var assemblyName =
        Assembly.GetExecutingAssembly().GetName().Name;

    var xmlPath = Path.Combine(
        AppContext.BaseDirectory,
        $"{assemblyName}.xml");

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(
            xmlPath,
            includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Вставте JWT-токен без префікса Bearer."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// Створення каталогів для завантажень
var webRootPath =
    builder.Environment.WebRootPath
    ?? Path.Combine(
        builder.Environment.ContentRootPath,
        "wwwroot");

Directory.CreateDirectory(webRootPath);

var uploadsRootPath = Path.IsPathRooted(storageOptions.RootPath)
    ? storageOptions.RootPath
    : storageOptions.RootPath.StartsWith(
        "wwwroot",
        StringComparison.OrdinalIgnoreCase)
        ? Path.Combine(
            builder.Environment.ContentRootPath,
            storageOptions.RootPath)
        : Path.Combine(
            webRootPath,
            storageOptions.RootPath);

uploadsRootPath = Path.GetFullPath(uploadsRootPath);

Directory.CreateDirectory(uploadsRootPath);
Directory.CreateDirectory(
    Path.Combine(uploadsRootPath, "products"));
Directory.CreateDirectory(
    Path.Combine(uploadsRootPath, "avatars"));
Directory.CreateDirectory(
    Path.Combine(uploadsRootPath, "categories"));

var uploadsRequestPath = storageOptions.BaseUrl;

if (Uri.TryCreate(
        uploadsRequestPath,
        UriKind.Absolute,
        out var absoluteStorageUrl))
{
    uploadsRequestPath = absoluteStorageUrl.AbsolutePath;
}

uploadsRequestPath =
    "/" + uploadsRequestPath.Trim('/');

if (uploadsRequestPath == "/")
{
    throw new InvalidOperationException(
        "Storage:BaseUrl має містити шлях, наприклад /uploads.");
}

var app = builder.Build();

// Middleware
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;

    await Results.Problem(
        statusCode: httpContext.Response.StatusCode,
        title: "Request failed",
        instance: httpContext.Request.Path.Value
    ).ExecuteAsync(httpContext);
});

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "AppleStore API v1");

    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});

app.UseRouting();

app.UseCors("Frontend");

// Статика uploads працює і для зовнішнього Storage:RootPath.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = uploadsRequestPath
});

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();

// Ініціалізація БД та seed.
// Використовуємо міграції, без змішування з EnsureCreated.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    if (!db.Database.GetMigrations().Any())
    {
        throw new InvalidOperationException(
            "Не знайдено міграцій EF Core. " +
            "У папці backend виконайте: " +
            "dotnet ef migrations add InitialCreate");
    }

    await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

await app.RunAsync();