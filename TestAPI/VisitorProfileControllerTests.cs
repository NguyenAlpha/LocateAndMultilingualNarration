using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Users;
using Xunit;

namespace TestAPI
{
    public class VisitorProfileControllerTests
    {
        [Fact]
        public async Task Create_And_Update_VisitorProfile()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid languageId;
            Guid anotherLanguageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);

                var languages = context.Languages.Where(l => l.IsActive).ToList();
                languageId = languages[0].Id;
                anotherLanguageId = languages[1].Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "visitor@test.com", "visitor", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = languageId
            });

            createResponse.EnsureSuccessStatusCode();

            var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResult<VisitorProfileDto>>(JsonOptions.Default);
            Assert.NotNull(createResult?.Data);

            var updateResponse = await client.PutAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = anotherLanguageId
            });

            updateResponse.EnsureSuccessStatusCode();

            var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResult<VisitorProfileDto>>(JsonOptions.Default);
            Assert.NotNull(updateResult?.Data);
            Assert.Equal(anotherLanguageId, updateResult.Data.LanguageId);
        }
    }
}
