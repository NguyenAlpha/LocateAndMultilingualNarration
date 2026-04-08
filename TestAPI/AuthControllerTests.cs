using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.Common;
using Xunit;

namespace TestAPI
{
    public class AuthControllerTests
    {
        // ===================== HAPPY PATH =====================

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
            Assert.Equal("owner@test.com", loginResult.Data.Email);

            var refreshResponse = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = loginResult.Data.RefreshToken,
                DeviceId = "test-device",
                ClientIp = "127.0.0.1"
            });

            refreshResponse.EnsureSuccessStatusCode();
            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResult<RefreshResponseDto>>(JsonOptions.Default);
            Assert.NotNull(refreshResult?.Data);
            Assert.False(string.IsNullOrWhiteSpace(refreshResult.Data.Token));

            var logoutResponse = await client.PostAsJsonAsync("/api/Auth/logout", new LogoutRequestDto
            {
                RefreshToken = refreshResult.Data.RefreshToken
            });

            logoutResponse.EnsureSuccessStatusCode();
            var logoutResult = await logoutResponse.Content.ReadFromJsonAsync<ApiResult<LogoutResponseDto>>(JsonOptions.Default);
            Assert.NotNull(logoutResult?.Data);
            Assert.True(logoutResult.Data.Success);
        }

        [Fact]
        public async Task Register_BusinessOwner_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var response = await client.PostAsJsonAsync("/api/Auth/register/business-owner", new RegisterBusinessOwnerDto
            {
                Email = "newowner@test.com",
                UserName = "newowner",
                Password = "Pass@123",
                PhoneNumber = "0912345678"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<RegisterResponseDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("newowner@test.com", result.Data.Email);
            Assert.Equal("newowner", result.Data.UserName);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedUserWithRole(context, "user@test.com", "user-test", "Pass@123", "BusinessOwner");
            }

            var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto
            {
                Email = "user@test.com",
                Password = "WrongPassword!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto
            {
                Email = "nobody@nowhere.com",
                Password = "Pass@123"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedUserWithRole(context, "existing@test.com", "existinguser", "Pass@123", "BusinessOwner");
            }

            var response = await client.PostAsJsonAsync("/api/Auth/register/business-owner", new RegisterBusinessOwnerDto
            {
                Email = "existing@test.com",
                UserName = "newname",
                Password = "Pass@123",
                PhoneNumber = "0900000000"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithInvalidToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = "invalid-token-that-does-not-exist",
                DeviceId = "test-device",
                ClientIp = "127.0.0.1"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_WithInvalidToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Auth/logout", new LogoutRequestDto
            {
                RefreshToken = "invalid-token-that-does-not-exist"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsCorrectRoles()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedUserWithRole(context, "admin@test.com", "admin-test", "Pass@123", "Admin");
            }

            var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto
            {
                Email = "admin@test.com",
                Password = "Pass@123"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<LoginResponseDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Contains("Admin", result.Data.Roles);
        }
    }
}
