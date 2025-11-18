using Firmeza.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.Entity<Sale>().Property(s => s.Total).HasPrecision(18, 2);
        builder.Entity<SaleItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Entity<SaleItem>().Property(i => i.Subtotal).HasPrecision(18, 2);

        builder.Entity<SaleItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SaleItem>()
            .HasOne(i => i.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SaleId);

        builder.Entity<ProductRequest>()
            .Property(r => r.Status)
            .HasMaxLength(32);

        builder.Entity<ProductRequest>()
            .Property(r => r.ResponseMessage)
            .HasMaxLength(500);

        builder.Entity<ProductRequest>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductRequest>()
            .HasOne(r => r.Sale)
            .WithMany()
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
