using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Firmeza.Web.Models;

namespace Firmeza.Web.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();
        public DbSet<ChatBotSettings> ChatBotSettings => Set<ChatBotSettings>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18,2);
            b.Entity<Sale>().Property(s => s.Total).HasPrecision(18,2);
            b.Entity<SaleItem>().Property(i => i.UnitPrice).HasPrecision(18,2);
            b.Entity<SaleItem>().Property(i => i.Subtotal).HasPrecision(18,2);
            b.Entity<ProductRequest>().Property(p => p.Status).HasMaxLength(32);
            b.Entity<ProductRequest>().Property(p => p.ResponseMessage).HasMaxLength(500);
            b.Entity<ProductRequest>()
                .HasOne(pr => pr.Product)
                .WithMany()
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
