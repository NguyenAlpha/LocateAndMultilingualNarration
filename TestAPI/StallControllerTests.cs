using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Stalls;
using Xunit;

namespace TestAPI
{
    public class StallControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_And_List_Stalls()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "stall-owner@test.com", "stall-owner", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Stall Business");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-owner@test.com", "stall-owner", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/Stall", new StallCreateDto
            {
                BusinessId = businessId,
                Name = "Test Stall",
                Description = "Test",
                Slug = "test-stall",
                ContactEmail = "stall@test.com",
                ContactPhone = "0900000001"
            });

            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(JsonOptions.Default);
            Assert.NotNull(createResult?.Data);
            Assert.Equal("test-stall", createResult.Data.Slug);

            var listResponse = await client.GetAsync($"/api/Stall?page=1&pageSize=10&businessId={businessId}");
            listResponse.EnsureSuccessStatusCode();
            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data.Items, s => s.Id == createResult.Data.Id);
        }

        [Fact]
        public async Task UpdateStall_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "stall-upd@test.com", "stall-upd", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Update Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Old Stall", "old-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-upd@test.com", "stall-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"/api/Stall/{stallId}", new StallUpdateDto
            {
                Name = "New Stall Name",
                Slug = "new-stall-slug",
                Description = "Updated description",
                IsActive = true
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("New Stall Name", result.Data.Name);
            Assert.Equal("new-stall-slug", result.Data.Slug);
        }

        [Fact]
        public async Task GetStallDetail_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "stall-det@test.com", "stall-det", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Detail Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "My Stall", "my-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-det@test.com", "stall-det", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Stall/{stallId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(stallId, result.Data.Id);
            Assert.Equal("My Stall", result.Data.Name);
        }

        [Fact]
        public async Task GetStalls_Search_ReturnsFiltered()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "stall-search@test.com", "stall-search", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Search Biz");
                TestDataSeeder.SeedStall(context, business.Id, "Pho Stall", "pho-stall");
                TestDataSeeder.SeedStall(context, business.Id, "Bun Bo Stall", "bun-bo-stall");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-search@test.com", "stall-search", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Stall?search=Pho&businessId={businessId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.All(result.Data.Items, s => Assert.Contains("Pho", s.Name));
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task CreateStall_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Stall", new StallCreateDto
            {
                BusinessId = Guid.NewGuid(),
                Name = "Unauthorized",
                Slug = "unauthorized"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateStall_DuplicateSlug_Returns409()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "stall-dup@test.com", "stall-dup", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Dup Biz");
                TestDataSeeder.SeedStall(context, business.Id, "Existing Stall", "existing-slug");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-dup@test.com", "stall-dup", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/Stall", new StallCreateDto
            {
                BusinessId = businessId,
                Name = "Another Stall",
                Slug = "existing-slug"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetStallDetail_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-stall-nf@test.com", "admin-stall-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Stall/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStallDetail_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "stall-own2@test.com", "stall-own2", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Protected Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Protected Stall", "protected-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-attacker@test.com", "stall-attacker", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Stall/{stallId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateStall_BusinessNotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "stall-nobiz@test.com", "stall-nobiz", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/Stall", new StallCreateDto
            {
                BusinessId = Guid.NewGuid(),
                Name = "No Business Stall",
                Slug = "no-biz"
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateStall_WithUserRole_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "user-stall@test.com", "user-stall", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/Stall", new StallCreateDto
            {
                BusinessId = Guid.NewGuid(),
                Name = "Forbidden Stall",
                Slug = "forbidden"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
