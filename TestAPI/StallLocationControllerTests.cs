using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.StallLocations;
using Xunit;

namespace TestAPI
{
    public class StallLocationControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_StallLocation_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "loc-owner@test.com", "loc-owner", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Location Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Location Stall", "location-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-owner@test.com", "loc-owner", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = stallId,
                Latitude = 10.762622m,
                Longitude = 106.660172m,
                RadiusMeters = 50m,
                Address = "123 Nguyen Hue, Q1, TP.HCM",
                IsActive = true
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(stallId, result.Data.StallId);
            Assert.Equal(10.762622m, result.Data.Latitude);
        }

        [Fact]
        public async Task Update_StallLocation_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "loc-upd@test.com", "loc-upd", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Loc Upd Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Loc Upd Stall", "loc-upd-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-upd@test.com", "loc-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = stallId,
                Latitude = 10.762622m,
                Longitude = 106.660172m,
                RadiusMeters = 50m,
                IsActive = true
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(JsonOptions.Default))!.Data!;

            var updateResponse = await client.PutAsJsonAsync($"/api/stall-location/{created.Id}", new StallLocationUpdateDto
            {
                Latitude = 10.800000m,
                Longitude = 106.700000m,
                RadiusMeters = 100m,
                Address = "456 Le Loi, Q1",
                IsActive = false
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(10.800000m, result.Data.Latitude);
            Assert.Equal(100m, result.Data.RadiusMeters);
            Assert.False(result.Data.IsActive);
        }

        [Fact]
        public async Task GetDetail_StallLocation_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "loc-det@test.com", "loc-det", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Loc Det Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Loc Det Stall", "loc-det-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-det@test.com", "loc-det", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = stallId,
                Latitude = 10.762622m,
                Longitude = 106.660172m,
                RadiusMeters = 30m,
                IsActive = true
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(JsonOptions.Default))!.Data!;

            var response = await client.GetAsync($"/api/stall-location/{created.Id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallLocationDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(created.Id, result.Data.Id);
        }

        [Fact]
        public async Task GetList_StallLocations_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "loc-list@test.com", "loc-list", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Loc List Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Loc List Stall", "loc-list-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-list@test.com", "loc-list", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = stallId, Latitude = 10.76m, Longitude = 106.66m, RadiusMeters = 50m, IsActive = true
            });

            var response = await client.GetAsync($"/api/stall-location?stallId={stallId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallLocationDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.TotalCount >= 1);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task CreateStallLocation_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = Guid.NewGuid(),
                Latitude = 10.76m,
                Longitude = 106.66m
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateStallLocation_StallNotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-nf@test.com", "loc-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = Guid.NewGuid(),
                Latitude = 10.76m,
                Longitude = 106.66m
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateStallLocation_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "loc-own2@test.com", "loc-own2", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Protected Loc Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Protected Stall", "protected-loc");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "loc-att@test.com", "loc-att", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-location", new StallLocationCreateDto
            {
                StallId = stallId,
                Latitude = 10.76m,
                Longitude = 106.66m
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetDetail_StallLocation_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-loc-nf@test.com", "admin-loc-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/stall-location/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
