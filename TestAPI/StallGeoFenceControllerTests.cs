using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.StallGeoFences;
using Xunit;

namespace TestAPI
{
    public class StallGeoFenceControllerTests
    {
        private const string SamplePolygon = """{"type":"Polygon","coordinates":[[[106.66,10.76],[106.67,10.76],[106.67,10.77],[106.66,10.77],[106.66,10.76]]]}""";

        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_GeoFence_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "geo-owner@test.com", "geo-owner", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Geo Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Geo Stall", "geo-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-owner@test.com", "geo-owner", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = stallId,
                PolygonJson = SamplePolygon,
                MinZoom = 14,
                MaxZoom = 20
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(stallId, result.Data.StallId);
            Assert.Equal(SamplePolygon, result.Data.PolygonJson);
            Assert.Equal(14, result.Data.MinZoom);
        }

        [Fact]
        public async Task Update_GeoFence_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "geo-upd@test.com", "geo-upd", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Geo Upd Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Geo Upd Stall", "geo-upd-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-upd@test.com", "geo-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = stallId,
                PolygonJson = SamplePolygon
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(JsonOptions.Default))!.Data!;

            const string updatedPolygon = """{"type":"Polygon","coordinates":[[[106.65,10.75],[106.68,10.75],[106.68,10.78],[106.65,10.78],[106.65,10.75]]]}""";

            var updateResponse = await client.PutAsJsonAsync($"/api/stall-geo-fence/{created.Id}", new StallGeoFenceUpdateDto
            {
                PolygonJson = updatedPolygon,
                MinZoom = 15,
                MaxZoom = 19
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(updatedPolygon, result.Data.PolygonJson);
            Assert.Equal(15, result.Data.MinZoom);
        }

        [Fact]
        public async Task GetDetail_GeoFence_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "geo-det@test.com", "geo-det", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Geo Det Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Geo Det Stall", "geo-det-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-det@test.com", "geo-det", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = stallId,
                PolygonJson = SamplePolygon
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(JsonOptions.Default))!.Data!;

            var response = await client.GetAsync($"/api/stall-geo-fence/{created.Id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallGeoFenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(created.Id, result.Data.Id);
        }

        [Fact]
        public async Task GetList_GeoFences_FilterByStall()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "geo-list@test.com", "geo-list", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Geo List Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Geo List Stall", "geo-list-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-list@test.com", "geo-list", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = stallId,
                PolygonJson = SamplePolygon
            });

            var response = await client.GetAsync($"/api/stall-geo-fence?stallId={stallId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallGeoFenceDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.TotalCount >= 1);
            Assert.All(result.Data.Items, g => Assert.Equal(stallId, g.StallId));
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task CreateGeoFence_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = Guid.NewGuid(),
                PolygonJson = SamplePolygon
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateGeoFence_StallNotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-nf@test.com", "geo-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = Guid.NewGuid(),
                PolygonJson = SamplePolygon
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateGeoFence_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "geo-own2@test.com", "geo-own2", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Protected Geo Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Protected Geo Stall", "protected-geo");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "geo-att@test.com", "geo-att", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-geo-fence", new StallGeoFenceCreateDto
            {
                StallId = stallId,
                PolygonJson = SamplePolygon
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetDetail_GeoFence_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-geo-nf@test.com", "admin-geo-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/stall-geo-fence/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
