using Microsoft.EntityFrameworkCore;
using Firmeza.WebApplication.Models;

namespace Firmeza.WebApplication.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(cfg =>
        {
            cfg.ToTable("Products");
            cfg.HasKey(p => p.Id);
            cfg.Property(p => p.Name).IsRequired().HasMaxLength(120);
            cfg.Property(p => p.UnitPrice).HasColumnType("numeric(18,2)");
            cfg.Property(p => p.IsActive).HasDefaultValue(true);
        });
    }
}
