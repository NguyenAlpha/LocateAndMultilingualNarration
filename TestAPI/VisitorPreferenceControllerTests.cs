using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.VisitorPreferences;
using Xunit;

namespace TestAPI
{
    public class VisitorPreferenceControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Upsert_CreatesVisitorPreference()
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
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-create@test.com", "vp-create", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = languageId,
                Voice = "vi-VN-Standard-A",
                SpeechRate = 1.0m,
                AutoPlay = true
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<VisitorPreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(languageId, result.Data.LanguageId);
            Assert.Equal("vi-VN-Standard-A", result.Data.Voice);
            Assert.True(result.Data.AutoPlay);
        }

        [Fact]
        public async Task Upsert_UpdatesExistingPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid viId, enId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                viId = context.Languages.First(l => l.Code == "vi").Id;
                enId = context.Languages.First(l => l.Code == "en").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-update@test.com", "vp-update", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = viId, SpeechRate = 1.0m
            });

            var updateResponse = await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = enId,
                Voice = "en-US-Standard-B",
                SpeechRate = 1.5m,
                AutoPlay = false
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<VisitorPreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(enId, result.Data.LanguageId);
            Assert.Equal(1.5m, result.Data.SpeechRate);
        }

        [Fact]
        public async Task GetDetail_ReturnsPreference()
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
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-get@test.com", "vp-get", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = languageId, SpeechRate = 1.0m
            });

            var response = await client.GetAsync("/api/visitor-preference");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<VisitorPreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(languageId, result.Data.LanguageId);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetDetail_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            // User chưa có preference
            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-nf@test.com", "vp-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/visitor-preference");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Upsert_InvalidLanguage_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "User", "vp-badlang@test.com", "vp-badlang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = Guid.NewGuid(), // không tồn tại
                SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Upsert_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/visitor-preference", new VisitorPreferenceUpsertDto
            {
                LanguageId = Guid.NewGuid(), SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
