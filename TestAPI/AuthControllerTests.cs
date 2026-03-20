using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.Common;
using Xunit;

namespace TestAPI
{
    // test luồng xác thực (authentication) bao gồm đăng nhập (login), làm mới token (refresh), và đăng xuất (logout)
    public class AuthControllerTests
    {
        [Fact]
        public async Task Login_Refresh_Logout_Works()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedUserWithRole(context, "owner@test.com", "owner-test", "Pass@123", "BusinessOwner");
            }

            var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto
            {
                Email = "owner@test.com",
                Password = "Pass@123"
            });

            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<LoginResponseDto>>(JsonOptions.Default);
            Assert.NotNull(loginResult?.Data);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.Data.RefreshToken));

            var refreshResponse = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = loginResult.Data.RefreshToken,
                DeviceId = "test-device",
                ClientIp = "127.0.0.1"
            });

            refreshResponse.EnsureSuccessStatusCode();

            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResult<RefreshResponseDto>>(JsonOptions.Default);
            Assert.NotNull(refreshResult?.Data);

            var logoutResponse = await client.PostAsJsonAsync("/api/Auth/logout", new LogoutRequestDto
            {
                RefreshToken = refreshResult.Data.RefreshToken
            });

            logoutResponse.EnsureSuccessStatusCode();

            var logoutResult = await logoutResponse.Content.ReadFromJsonAsync<ApiResult<LogoutResponseDto>>(JsonOptions.Default);
            Assert.NotNull(logoutResult?.Data);
            Assert.True(logoutResult.Data.Success);
        }
    }
}
