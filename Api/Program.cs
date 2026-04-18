using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Persistence;
using Api.Application.Services;
using Api.Authorization;
using Api.Domain.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Cấu hình Console để hiển thị tiếng Việt đúng
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Khởi tạo builder để cấu hình toàn bộ ứng dụng
var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình JWT từ appsettings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (string.IsNullOrWhiteSpace(jwtSettings?.Key) || jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("JWT Key phải được cấu hình và có độ dài tối thiểu 32 ký tự.");

// Đọc cấu hình Azure Speech và Blob Storage cho TTS
builder.Services.Configure<AzureSpeechSettings>(builder.Configuration.GetSection("AzureSpeech"));
builder.Services.Configure<BlobStorageSettings>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<AzureTranslationSettings>(builder.Configuration.GetSection("AzureTranslation"));

// Đăng ký các service nghiệp vụ vào DI container
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IGeoService, GeoService>();
builder.Services.AddScoped<INarrationAudioService, NarrationAudioService>();
builder.Services.AddHttpClient<ITranslationService, AzureTranslationService>();
builder.Services.AddHostedService<TtsBackgroundService>();

// Cấu hình xác thực JWT cho toàn bộ API
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Kiểm tra issuer, audience, thời hạn token và chữ ký
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.Key ?? string.Empty)),
        ClockSkew = TimeSpan.Zero
    };
});

// Bật authorization policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy(AppPolicies.AdminOrBusinessOwner, policy =>
        policy.RequireRole("Admin", "BusinessOwner"));
});

// Cấu hình CORS để cho phép Web project gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7298",    // Web HTTPS
                "http://localhost:5209",     // Web HTTP
                "https://localhost:7188",    // API HTTPS (self)
                "http://localhost:5299"      // API HTTP (self)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Đăng ký controller API
builder.Services.AddControllers();

// Bật OpenAPI/Swagger
builder.Services.AddOpenApi();

// Đăng ký DbContext để inject vào controller/service
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("default");

    // Chỉ dùng SQL Server khi có connection string
    if (!string.IsNullOrEmpty(cs))
        options.UseSqlServer(cs);
});

// Build ứng dụng sau khi đã cấu hình xong
var app = builder.Build();

// Khi ở Development thì bật OpenAPI và Swagger UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        // Trỏ Swagger UI tới tài liệu OpenAPI đã map ở trên
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
        // Tùy chọn UI thêm:
        options.EnablePersistAuthorization();
        options.DisplayRequestDuration();
    });

}

// Middleware log các request bị 401 để dễ debug auth
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

// Chạy migration và seed dữ liệu mặc định nếu có connection string
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("default")))
    {
        try
        {
            db.Database.Migrate();
            await DbSeeder.SeedAsync(db);
        }
        catch (SqlException ex)
        {
            // OLD CODE (kept for reference): db.Database.Migrate(); await DbSeeder.SeedAsync(db);
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Không thể kết nối SQL Server khi startup. API vẫn chạy, hãy kiểm tra DB rồi chạy migration lại.");
        }
        catch (Exception ex)
        {
            // OLD CODE (kept for reference): chỉ bắt SqlException.
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Migration khi startup thất bại (ví dụ PendingModelChanges). API vẫn chạy để phục vụ debug.");
        }
    }
}

// Chuyển HTTP sang HTTPS (tắt ở Development để emulator/mobile dùng HTTP)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Bật CORS middleware (phải chạy trước Authentication)
app.UseCors("AllowWeb");

// Authentication phải chạy trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map toàn bộ controller routes
app.MapControllers();

// Chạy ứng dụng
app.Run();

// Cho phép test integration sử dụng Program class này
public partial class Program {}