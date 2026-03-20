using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Languages;
using Xunit;

namespace TestAPI
{
    public class LanguageControllerTests
    {
        [Fact]
        public async Task Create_And_List_Active_Languages()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin@test.com", "admin", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto
            {
                Name = "French",
                Code = "fr",
                IsActive = true
            });

            createResponse.EnsureSuccessStatusCode();

            var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default);
            Assert.NotNull(createResult?.Data);

            client.DefaultRequestHeaders.Authorization = null;

            var listResponse = await client.GetAsync("/api/languages/active");
            listResponse.EnsureSuccessStatusCode();

            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data, l => l.Code == "fr");
        }
    }
}
