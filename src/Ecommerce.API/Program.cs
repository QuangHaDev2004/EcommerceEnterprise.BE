// [nqha][2026-11-23]: Điểm khởi chạy API cấu hình Dependency Injection, CORS, Swagger, HealthCheck và DbContext
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext (Hỗ trợ SQL Server hoặc InMemory cho Development)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(connectionString))
    {
        options.UseInMemoryDatabase("EcommerceDevDb");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 2. Cấu hình MediatR & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IAppDbContext).Assembly));

// 3. Cấu hình CORS - Cho phép Frontend Next.js với withCredentials: true
const string corsPolicyName = "AllowFrontendApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. Cấu hình Health Checks
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ecommerce Enterprise API", Version = "v1" });
});

var app = builder.Build();

// 5. Cấu hình HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce Enterprise API v1"));
}

app.UseCors(corsPolicyName);

// 6. Endpoint Health Check chuẩn JSON cho Frontend và kịch bản Playwright
app.MapGet("/health", async (AppDbContext dbContext) =>
{
    bool dbCanConnect = false;
    try
    {
        dbCanConnect = await dbContext.Database.CanConnectAsync();
    }
    catch
    {
        dbCanConnect = false;
    }

    return Results.Ok(new
    {
        status = "Healthy",
        database = dbCanConnect ? "Connected" : "Standby/Disconnected",
        timestamp = DateTime.UtcNow
    });
});

app.MapControllers();

app.Run();
