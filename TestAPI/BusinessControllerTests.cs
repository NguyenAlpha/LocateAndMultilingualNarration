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

            var listResponse = await client.GetAsync("/api/Business?page=1&pageSize=10");
            listResponse.EnsureSuccessStatusCode();

            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PagedResult<BusinessDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data.Items, b => b.Id == createResult.Data.Id);
        }
    }
}
