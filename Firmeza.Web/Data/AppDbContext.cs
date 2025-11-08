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
        public DbSet<ChatBotSettings> ChatBotSettings => Set<ChatBotSettings>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18,2);
            b.Entity<Sale>().Property(s => s.Total).HasPrecision(18,2);
            b.Entity<SaleItem>().Property(i => i.UnitPrice).HasPrecision(18,2);
            b.Entity<SaleItem>().Property(i => i.Subtotal).HasPrecision(18,2);
        }
    }
}
