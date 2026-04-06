using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.VisitorLocationLogs;
using Xunit;

namespace TestAPI
{
    public class VisitorLocationLogControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_LocationLog_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "log-create@test.com", "log-create", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
            {
                Latitude = 10.762622m,
                Longitude = 106.660172m,
                AccuracyMeters = 10m
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<VisitorLocationLogDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(10.762622m, result.Data.Latitude);
            Assert.Equal(106.660172m, result.Data.Longitude);
        }

        [Fact]
        public async Task GetList_ReturnsOwnLogs()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "log-list@test.com", "log-list", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
            {
                Latitude = 10.76m, Longitude = 106.66m, AccuracyMeters = 5m
            });
            await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
            {
                Latitude = 10.77m, Longitude = 106.67m, AccuracyMeters = 8m
            });

            var response = await client.GetAsync("/api/visitor-location-log");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<VisitorLocationLogDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(2, result.Data.TotalCount);
        }

        [Fact]
        public async Task GetList_Admin_CanQueryOtherUserId()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            // Tạo log với user thường
            Guid targetUserId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "log-target@test.com", "log-target", "Pass@123", "User");
                targetUserId = user.Id;
            }

            var (userToken, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "log-target@test.com", "log-target", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
            {
                Latitude = 10.76m, Longitude = 106.66m
            });

            // Admin query log của user khác
            var (adminToken, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "log-admin@test.com", "log-admin", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var response = await client.GetAsync($"/api/visitor-location-log?userId={targetUserId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<VisitorLocationLogDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.TotalCount >= 1);
        }

        [Fact]
        public async Task GetList_Pagination_Works()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "log-page@test.com", "log-page", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            for (int i = 0; i < 5; i++)
            {
                await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
                {
                    Latitude = 10.76m + i * 0.001m, Longitude = 106.66m
                });
            }

            var response = await client.GetAsync("/api/visitor-location-log?page=1&pageSize=3");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<VisitorLocationLogDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(5, result.Data.TotalCount);
            Assert.Equal(3, result.Data.Items.Count);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task Create_LocationLog_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/visitor-location-log", new VisitorLocationLogCreateDto
            {
                Latitude = 10.76m, Longitude = 106.66m
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetList_WithOtherUserId_AsNonAdmin_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid otherUserId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var other = TestDataSeeder.SeedUserWithRole(context, "log-other@test.com", "log-other", "Pass@123", "User");
                otherUserId = other.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "log-nonadmin@test.com", "log-nonadmin", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/visitor-location-log?userId={otherUserId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
