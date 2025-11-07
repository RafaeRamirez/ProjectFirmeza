// Framework
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// App
using Firmeza.Web.Data;
using Firmeza.Web.Models;
using Firmeza.Web.Interfaces;
using Firmeza.Web.Repositories;
using Firmeza.Web.Services;
using Firmeza.Web.Utils;

var builder = WebApplication.CreateBuilder(args);

// 1) .env en Development
if (builder.Environment.IsDevelopment())
{
    try { Env.Load(); } catch { /* ignore */ }
}

// 2) Connection string: ENV -> appsettings -> fallback local
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
         ?? builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=FirmezaDb;Username=postgres;Password=123456;Ssl Mode=Prefer";

// 3) DbContext (EF Core + Npgsql)
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(cs);
    if (builder.Environment.IsDevelopment())
        opt.EnableSensitiveDataLogging();
});

// 4) Identity + Roles
builder.Services
    .AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireDigit = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 5) Autorización por política
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
});

// 6) Cookies de autenticación
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

// 7) MVC
builder.Services.AddControllersWithViews();

// 8) DI de Repos/Servicios/Utils
builder.Services.AddScoped<IStringSanitizer, StringSanitizer>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IPdfService, PdfService>();

var app = builder.Build();

// 9) Pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 10) Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 11) DB Ensure + Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// 12) *** FIX del crash: calcular WebRoot seguro y crear receipts ***
var webRoot = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRoot))
{
    // Si no hay wwwroot, usar ContentRoot/wwwroot y crearlo
    webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
}
Directory.CreateDirectory(webRoot);                          // asegura wwwroot
Directory.CreateDirectory(Path.Combine(webRoot, "receipts"));// asegura /wwwroot/receipts

app.Run();
