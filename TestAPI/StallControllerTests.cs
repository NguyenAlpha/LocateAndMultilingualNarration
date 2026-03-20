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

            var listResponse = await client.GetAsync($"/api/Stall?page=1&pageSize=10&businessId={businessId}");
            listResponse.EnsureSuccessStatusCode();

            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data.Items, s => s.Id == createResult.Data.Id);
        }
    }
}
