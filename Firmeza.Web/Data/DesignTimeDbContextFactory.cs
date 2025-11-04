// File: Data/DesignTimeDbContextFactory.cs
// Purpose: Allow 'dotnet ef' to create AppDbContext without running Program.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Firmeza.WebApplication.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Try to load .env in design-time; ignore if not present
        try { DotNetEnv.Env.Load(); } catch { /* ignore */ }

        // Fallback string ensures no "Missing DB connection string" during design-time
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Port=5432;Database=FirmezaDb;Username=postgres;Password=123456;Ssl Mode=Prefer";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
