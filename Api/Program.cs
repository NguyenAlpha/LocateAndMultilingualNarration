using System;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Persistence;
using Api.Application.Services;
using Api.Domain.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Cấu hình Console để hiển thị tiếng Việt đúng
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 1. Bind JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

// 2. Register JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// 3. Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.Key ?? string.Empty))
    };
});

// 4. Configure Authorization
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Thiết lập kết nối database cho ứng dụng:
// Đăng ký AppDbContext vào DI container của ASP.NET Core.
// - AddDbContext<T>: cho phép bạn inject AppDbContext vào controller/service thông qua constructor.
// - options.UseSqlServer(...): chỉ định EF Core sử dụng SQL Server làm provider.
// - builder.Configuration.GetConnectionString("default"): lấy chuỗi kết nối có tên "default"
//   từ cấu hình (ví dụ appsettings.json -> "ConnectionStrings": { "default": "..." }).
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("default");
    // Chỉ cấu hình SQL Server nếu có connection string, để tránh lỗi khi chạy mà không có DB (như trong test)
    if (!string.IsNullOrEmpty(cs))
        options.UseSqlServer(cs);
});

// Build app (tạo ứng dụng từ các cấu hình trên)
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware để log các request trả về 401 Unauthorized
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Unauthorized(401) request to {Path} from {RemoteIpAddress}",
            context.Request.Path, context.Connection.RemoteIpAddress);
    }
});

// Chỉ chạy migrate khi có connection string
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("default")))
        db.Database.Migrate();
}

// Middleware chuyển hướng HTTP sang HTTPS để đảm bảo bảo mật
app.UseHttpsRedirection();

app.UseAuthentication();  // Phải đặt trước UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program{}