// [nqha][2026-11-23]: Điểm khởi chạy File Storage Microservice lắng nghe cổng 5002 và cấu hình StaticFiles Cache-Control
using Ecommerce.FileManager.API.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ DI
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// 2. Cấu hình CORS - Cho phép Frontend Next.js gọi upload
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ecommerce File Storage Microservice", Version = "v1" });
});

var app = builder.Build();

// 3. Đảm bảo thư mục wwwroot/uploads tồn tại
var wwwrootDir = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootDir))
{
    Directory.CreateDirectory(wwwrootDir);
}
var uploadPath = Path.Combine(wwwrootDir, "uploads");
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

// 4. Cấu hình phục vụ file tĩnh với HTTP Cache-Control (Cache 1 năm)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootDir),
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        // Cache tối ưu cho hình ảnh WebP tĩnh
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce File Storage v1"));
}

app.UseCors(corsPolicyName);

// 5. Endpoint Health Check
app.MapGet("/health", () =>
{
    var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "C:\\");
    return Results.Ok(new
    {
        service = "Ecommerce.FileManager.API",
        status = "Healthy",
        freeDiskSpaceMb = driveInfo.AvailableFreeSpace / 1024 / 1024,
        timestamp = DateTime.UtcNow
    });
});

app.MapControllers();

app.Run();
