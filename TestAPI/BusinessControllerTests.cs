using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Businesses;
using Shared.DTOs.Common;
using Xunit;

namespace TestAPI
{
    public class BusinessControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_And_List_Businesses()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo@test.com", "bo-test", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto
            {
                Name = "Test Business",
                TaxCode = "TAX-001",
                ContactEmail = "contact@test.com",
                ContactPhone = "0900000000"
            });

            createResponse.EnsureSuccessStatusCode();
            var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(JsonOptions.Default);
            Assert.NotNull(createResult?.Data);
            Assert.Equal("Test Business", createResult.Data.Name);

            var listResponse = await client.GetAsync("/api/Business?page=1&pageSize=10");
            listResponse.EnsureSuccessStatusCode();
            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data.Items, b => b.Id == createResult.Data.Id);
        }

        [Fact]
        public async Task UpdateBusiness_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-update@test.com", "bo-update", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto
            {
                Name = "Old Name",
                TaxCode = "TAX-002"
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(JsonOptions.Default))!.Data!;

            var updateResponse = await client.PutAsJsonAsync($"/api/Business/{created.Id}", new BusinessUpdateDto
            {
                Name = "New Name",
                TaxCode = "TAX-002-UPDATED",
                ContactEmail = "new@test.com",
                ContactPhone = "0900000002",
                IsActive = true
            });

            updateResponse.EnsureSuccessStatusCode();
            var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(JsonOptions.Default);
            Assert.NotNull(updateResult?.Data);
            Assert.Equal("New Name", updateResult.Data.Name);
            Assert.Equal("TAX-002-UPDATED", updateResult.Data.TaxCode);
        }

        [Fact]
        public async Task GetBusinessDetail_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "bo-detail@test.com", "bo-detail", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Detail Business");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-detail@test.com", "bo-detail", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Business/{businessId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<BusinessDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(businessId, result.Data.Id);
            Assert.Equal("Detail Business", result.Data.Name);
        }

        [Fact]
        public async Task GetBusinesses_Search_ReturnsFiltered()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-search@test.com", "bo-search", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto { Name = "Pho Ha Noi", TaxCode = "T1" });
            await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto { Name = "Bun Bo Hue", TaxCode = "T2" });

            var response = await client.GetAsync("/api/Business?search=Pho");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.All(result.Data.Items, b => Assert.Contains("Pho", b.Name));
        }

        [Fact]
        public async Task Admin_CanListAllBusinesses()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "owner-admin-list@test.com", "owner-admin-list", "Pass@123", "BusinessOwner");
                TestDataSeeder.SeedBusiness(context, owner.Id, "Owner's Biz");
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-list@test.com", "admin-list", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/Business?page=1&pageSize=100");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.TotalCount >= 1);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetBusinessDetail_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-nf@test.com", "admin-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Business/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetBusinessDetail_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "real-owner@test.com", "real-owner", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Owner's Business");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "other-owner@test.com", "other-owner", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Business/{businessId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateBusiness_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto
            {
                Name = "Unauthorized Business"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateBusiness_WithUserRole_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "regular@test.com", "regular-user", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/Business", new BusinessCreateDto
            {
                Name = "Forbidden Business"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBusiness_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid businessId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "owner-upd@test.com", "owner-upd", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Protected Business");
                businessId = business.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "attacker@test.com", "attacker", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"/api/Business/{businessId}", new BusinessUpdateDto
            {
                Name = "Hacked!", IsActive = true
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBusiness_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-upd@test.com", "admin-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"/api/Business/{Guid.NewGuid()}", new BusinessUpdateDto
            {
                Name = "Ghost", IsActive = true
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
