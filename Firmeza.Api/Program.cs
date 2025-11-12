using System.Text;
using DotNetEnv;
using Firmeza.Api.Data;
using Firmeza.Api.Domain.Entities;
using Firmeza.Api.Domain.Settings;
using Firmeza.Api.Mapping;
using Firmeza.Api.Services;
using Firmeza.Api.Services.Abstractions;
using Firmeza.Api.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    try { Env.Load(); } catch { /* ignore missing .env */ }
}

var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
         ?? builder.Configuration.GetConnectionString("Default")
         ?? "Host=localhost;Port=5432;Database=FirmezaDb;Username=postgres;Password=123456;Ssl Mode=Prefer";

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(cs);
    if (builder.Environment.IsDevelopment())
    {
        opt.EnableSensitiveDataLogging();
    }
});

builder.Services
    .AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 6;
        o.Password.RequireDigit = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey") ?? throw new InvalidOperationException("Debe configurar Jwt:SigningKey");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireAdmin", p => p.RequireRole("Admin", "SuperAdmin"));
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

builder.Services.AddAutoMapper(typeof(DomainProfile).Assembly);
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Host"]))
{
    builder.Services.AddSingleton<IEmailSender, NullEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Firmeza API",
        Version = "v1",
        Description = "API REST para gestionar productos, clientes y ventas."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese un token JWT válido.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DbSeeder.SeedAsync(app.Services);

app.Run();
