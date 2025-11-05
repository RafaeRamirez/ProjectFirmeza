
using Firmeza.WebApplication.Data;
using Firmeza.WebApplication.Interfaces;
using Firmeza.WebApplication.Repositories;
using Firmeza.WebApplication.Services;
using Firmeza.WebApplication.Utils;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    try { DotNetEnv.Env.Load(); } catch { /* ignore if not present */ }
}

var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
         ?? builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=FirmezaDb;Username=postgres;Password=123456;Ssl Mode=Prefer";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(cs);
    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<IStringSanitizer, StringSanitizer>();
builder.Services.AddHttpClient<IAiChatService, GeminiAiService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
