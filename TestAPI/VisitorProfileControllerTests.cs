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
    public class VisitorProfileControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_And_Update_VisitorProfile()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid languageId, anotherLanguageId;
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
            Assert.Equal(languageId, createResult.Data.LanguageId);

            var updateResponse = await client.PutAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = anotherLanguageId
            });

            updateResponse.EnsureSuccessStatusCode();
            var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResult<VisitorProfileDto>>(JsonOptions.Default);
            Assert.NotNull(updateResult?.Data);
            Assert.Equal(anotherLanguageId, updateResult.Data.LanguageId);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task CreateProfile_Duplicate_Returns409()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                languageId = context.Languages.First(l => l.IsActive).Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-dup@test.com", "vp-dup", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto { LanguageId = languageId });

            // Tạo lần 2 – phải trả 409
            var response = await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto { LanguageId = languageId });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateProfile_InvalidLanguage_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-badlang@test.com", "vp-badlang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = Guid.NewGuid() // không tồn tại
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                languageId = context.Languages.First(l => l.IsActive).Id;
            }

            // User chưa tạo profile → PUT phải trả 404
            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-upd-nf@test.com", "vp-upd-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = languageId
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateProfile_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = Guid.NewGuid()
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProfile_WithInactiveLanguage_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid inactiveLangId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                inactiveLangId = context.Languages.First(l => !l.IsActive).Id; // "ja" is inactive in seed
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-inactive@test.com", "vp-inactive", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/visitor-profile", new VisitorProfileUpdateDto
            {
                LanguageId = inactiveLangId
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
