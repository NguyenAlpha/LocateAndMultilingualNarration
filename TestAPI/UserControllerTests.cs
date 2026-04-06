using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Users;
using Xunit;

namespace TestAPI
{
    public class UserControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Get_User_Detail_Returns_Self()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, userId) = await TestAuthHelper.LoginAsync(factory, client, "User", "user@test.com", "user", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/User/{userId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<UserDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(userId, result.Data.Id);
            Assert.Equal("user@test.com", result.Data.Email);
        }

        [Fact]
        public async Task Admin_CanViewAnyUser()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid targetUserId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var target = TestDataSeeder.SeedUserWithRole(context, "user-target@test.com", "user-target", "Pass@123", "User");
                targetUserId = target.Id;
            }

            var (adminToken, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-user@test.com", "admin-user", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await client.GetAsync($"/api/User/{targetUserId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<UserDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(targetUserId, result.Data.Id);
        }

        [Fact]
        public async Task Get_User_Detail_ReturnsRoles()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, userId) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-userdet@test.com", "bo-userdet", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/User/{userId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<UserDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Contains("BusinessOwner", result.Data.Roles);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetUserDetail_OtherUser_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid otherUserId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var other = TestDataSeeder.SeedUserWithRole(context, "other-user@test.com", "other-user", "Pass@123", "User");
                otherUserId = other.Id;
            }

            // Đăng nhập với user khác
            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "self-user@test.com", "self-user", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/User/{otherUserId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserDetail_NonExistentId_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-user-nf@test.com", "admin-user-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/User/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUserDetail_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync($"/api/User/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
