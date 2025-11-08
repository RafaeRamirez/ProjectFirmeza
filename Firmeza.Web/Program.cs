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
using QuestPDF.Infrastructure;

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

builder.Services.Configure<SecurityStampValidatorOptions>(o =>
{
    // Revalida la cookie en cada request para que los nuevos roles apliquen al instante
    o.ValidationInterval = TimeSpan.Zero;
});

// 5) Autorización por política
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireAdmin", p => p.RequireRole("Admin", "SuperAdmin"));
    opt.AddPolicy("RequireSuperAdmin", p => p.RequireRole("SuperAdmin"));
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

var emailSection = builder.Configuration.GetSection("Email");
builder.Services.Configure<EmailSettings>(emailSection);
if (string.IsNullOrWhiteSpace(emailSection["Host"]))
{
    builder.Services.AddSingleton<IEmailSender, NullEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
}

builder.Services.AddScoped<IChatBotSettingsProvider, DbChatBotSettingsProvider>();
builder.Services.AddHttpClient<GoogleApiClient>();
builder.Services.AddHttpClient<IChatBotService, GoogleAiChatBotService>();

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
    await EnsureLegacySchemaFixesAsync(db);
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// 12) *** FIX del crash: calcular WebRoot seguro y crear receipts ***
var webRoot = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRoot))
{
    webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    app.Environment.WebRootPath = webRoot;
}
Directory.CreateDirectory(webRoot);                          // asegura wwwroot
Directory.CreateDirectory(Path.Combine(webRoot, "receipts"));// asegura /wwwroot/receipts

if (QuestPDF.Settings.License != LicenseType.Community)
{
    QuestPDF.Settings.License = LicenseType.Community;
}

app.Run();

static async Task EnsureLegacySchemaFixesAsync(AppDbContext db)
{
    const string customersSql = """
DO $$
DECLARE
    target_table text;
    info_name text;
BEGIN
    IF to_regclass('public."Customers"') IS NOT NULL THEN
        target_table := 'public."Customers"';
        info_name := 'Customers';
    ELSIF to_regclass('public.customers') IS NOT NULL THEN
        target_table := 'public.customers';
        info_name := 'customers';
    END IF;

    IF target_table IS NOT NULL THEN
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = info_name
              AND column_name = 'Name')
           AND NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = info_name
              AND column_name = 'FullName') THEN
            EXECUTE format('ALTER TABLE %s RENAME COLUMN "Name" TO "FullName";', target_table);
        ELSIF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = info_name
              AND column_name = 'FullName') THEN
            EXECUTE format('ALTER TABLE %s ADD COLUMN "FullName" text NOT NULL DEFAULT '''';', target_table);
        END IF;
    END IF;
END $$;
""";

    const string productsSql = """
DO $$
DECLARE
    target_table text;
BEGIN
    IF to_regclass('public."Products"') IS NOT NULL THEN
        target_table := 'public."Products"';
    ELSIF to_regclass('public.products') IS NOT NULL THEN
        target_table := 'public.products';
    END IF;

    IF target_table IS NOT NULL THEN
        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;', target_table);
        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS "Stock" integer NOT NULL DEFAULT 0;', target_table);
    END IF;
END $$;
""";

    const string customersOwnerSql = """
DO $$
DECLARE
    table_name text;
BEGIN
    IF to_regclass('public."Customers"') IS NOT NULL THEN
        table_name := 'public."Customers"';
    ELSIF to_regclass('public.customers') IS NOT NULL THEN
        table_name := 'public.customers';
    END IF;

    IF table_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS "CreatedByUserId" text NOT NULL DEFAULT '''';', table_name);
    END IF;
END $$;
""";

    const string salesOwnerSql = """
DO $$
DECLARE
    table_name text;
BEGIN
    IF to_regclass('public."Sales"') IS NOT NULL THEN
        table_name := 'public."Sales"';
    ELSIF to_regclass('public.sales') IS NOT NULL THEN
        table_name := 'public.sales';
    END IF;

    IF table_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS "CreatedByUserId" text NOT NULL DEFAULT '''';', table_name);
    END IF;
END $$;
""";

    const string productsOwnerSql = """
DO $$
DECLARE
    table_name text;
BEGIN
    IF to_regclass('public."Products"') IS NOT NULL THEN
        table_name := 'public."Products"';
    ELSIF to_regclass('public.products') IS NOT NULL THEN
        table_name := 'public.products';
    END IF;

    IF table_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS "CreatedByUserId" text NOT NULL DEFAULT '''';', table_name);
    END IF;
END $$;
""";

    const string chatBotTableSql = """
CREATE TABLE IF NOT EXISTS "ChatBotSettings"
(
    "Id" SERIAL PRIMARY KEY,
    "ApiKey" text NOT NULL DEFAULT '',
    "Model" text NOT NULL DEFAULT 'models/gemini-1.5-flash',
    "Scope" text NOT NULL DEFAULT 'https://www.googleapis.com/auth/cloud-platform',
    "ServiceAccountJsonPath" text NOT NULL DEFAULT '',
    "Endpoint" text NOT NULL DEFAULT 'https://generativelanguage.googleapis.com',
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT NOW()
);
""";

    const string chatBotColumnsSql = """
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "ApiKey" text NOT NULL DEFAULT '';
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "Model" text NOT NULL DEFAULT 'models/gemini-1.5-flash';
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "Scope" text NOT NULL DEFAULT 'https://www.googleapis.com/auth/cloud-platform';
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "ServiceAccountJsonPath" text NOT NULL DEFAULT '';
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "Endpoint" text NOT NULL DEFAULT 'https://generativelanguage.googleapis.com';
ALTER TABLE "ChatBotSettings" ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamp without time zone NOT NULL DEFAULT NOW();
""";

    await db.Database.ExecuteSqlRawAsync(customersSql);
    await db.Database.ExecuteSqlRawAsync(productsSql);
    await db.Database.ExecuteSqlRawAsync(customersOwnerSql);
    await db.Database.ExecuteSqlRawAsync(salesOwnerSql);
    await db.Database.ExecuteSqlRawAsync(productsOwnerSql);
    await db.Database.ExecuteSqlRawAsync(chatBotTableSql);
    await db.Database.ExecuteSqlRawAsync(chatBotColumnsSql);
}
