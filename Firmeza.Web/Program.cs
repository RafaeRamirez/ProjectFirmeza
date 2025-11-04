
using Firmeza.WebApplication.Data;
using Firmeza.WebApplication.Interfaces;
using Firmeza.WebApplication.Repositories;
using Firmeza.WebApplication.Services;
using Firmeza.WebApplication.Utils;

// Framework
using Microsoft.EntityFrameworkCore;

// --- Host builder (must be first line after usings) ---
var builder = WebApplication.CreateBuilder(args);

// Load .env only in Development (never commit secrets)
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

// Resolve connection string: ENV -> appsettings -> local fallback (design-time safe)
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
         ?? builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=FirmezaDb;Username=postgres;Password=123456;Ssl Mode=Prefer";

// EF Core / Npgsql
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(cs));

// DI registrations
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<IStringSanitizer, StringSanitizer>();
builder.Services.AddHttpClient<IAiChatService, GeminiAiService>();

// MVC
builder.Services.AddControllersWithViews();

// --- Build app ---
var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // app.UseHsts(); // optional in production
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
