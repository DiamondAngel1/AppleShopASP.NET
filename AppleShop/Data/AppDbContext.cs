namespace AppleShop.Data;

using AppleShop.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Product>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(400).IsRequired();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.OldPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Category).WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductImage>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.Property(x => x.ThumbnailUrl).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Product).WithMany(p => p.Images)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductVariant>(e =>
        {
            e.Property(x => x.Color).HasMaxLength(60).IsRequired();
            e.Property(x => x.PriceDelta).HasPrecision(18, 2);
            e.HasOne(x => x.Product).WithMany(p => p.Variants)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductSpec>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(120).IsRequired();
            e.Property(x => x.Value).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Product).WithMany(p => p.Specs)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Cart>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.Cart)
                .HasForeignKey<Cart>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasOne(x => x.Cart).WithMany(c => c.Items)
                .HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Variant).WithMany()
                .HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Order>(e =>
        {
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.Address).HasMaxLength(300).IsRequired();
            e.HasOne(x => x.User).WithMany(u => u.Orders)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Review>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(1500).IsRequired();
            e.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
            e.HasOne(x => x.Product).WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Reviews)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Favorite>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ProductId });
            e.HasOne(x => x.User).WithMany(u => u.Favorites)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}