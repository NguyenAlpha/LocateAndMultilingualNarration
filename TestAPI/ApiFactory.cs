using Api.Infrastructure.Persistence;  // Tham chiếu đến namespace chứa AppDbContext (Entity Framework DbContext)
using Microsoft.AspNetCore.Hosting;     // Cung cấp IWebHostBuilder để cấu hình web host
using Microsoft.AspNetCore.Mvc.Testing; // Cung cấp WebApplicationFactory để test API
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;    // Sử dụng Entity Framework cho database operations
using Microsoft.Extensions.Configuration; // Để cấu hình appsettings
using Microsoft.Extensions.DependencyInjection; // Để đăng ký và quản lý dịch vụ (DI)

namespace TestAPI
{
    // ApiFactory kế thừa từ WebApplicationFactory<Program> để tạo môi trường test cho API
    // Nó giúp khởi tạo và chạy ứng dụng ASP.NET Core trong bộ nhớ mà không cần server thật
    public class ApiFactory : WebApplicationFactory<Program>
    {
        // Mỗi test run có 1 DB riêng để tránh rò rỉ dữ liệu giữa các test
        private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";
        
        // Phương thức override để cấu hình WebHost trước khi khởi tạo
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Chỉ thêm cấu hình phục vụ test
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Ép vô hiệu connection string "default" để Program.cs không dùng SQL Server khi test
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:default"] = null,
                    // Override LogLevel để giảm log noise trong test
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                    ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning",
                    ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // Đăng ký InMemory với tên DB ngẫu nhiên cho run hiện tại
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        }
    }
}