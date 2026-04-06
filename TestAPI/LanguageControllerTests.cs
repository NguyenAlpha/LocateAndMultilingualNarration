using System.Net;
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
        // ===================== HAPPY PATH =====================

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
            Assert.Equal("fr", createResult.Data.Code);

            client.DefaultRequestHeaders.Authorization = null;

            var listResponse = await client.GetAsync("/api/languages/active");
            listResponse.EnsureSuccessStatusCode();
            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Contains(listResult.Data, l => l.Code == "fr");
        }

        [Fact]
        public async Task UpdateLanguage_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-lang-upd@test.com", "admin-lang-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto
            {
                Name = "German",
                Code = "de",
                IsActive = true
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default))!.Data!;

            var updateResponse = await client.PutAsJsonAsync($"/api/languages/{created.Id}", new LanguageUpdateDto
            {
                Name = "Deutsch",
                Code = "de",
                IsActive = false
            });

            updateResponse.EnsureSuccessStatusCode();
            var updateResult = await updateResponse.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default);
            Assert.NotNull(updateResult?.Data);
            Assert.Equal("Deutsch", updateResult.Data.Name);
            Assert.False(updateResult.Data.IsActive);
        }

        [Fact]
        public async Task DeactivateLanguage_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-lang-del@test.com", "admin-lang-del", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto
            {
                Name = "Spanish",
                Code = "es",
                IsActive = true
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default))!.Data!;

            var deleteResponse = await client.DeleteAsync($"/api/languages/{created.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default);
            Assert.NotNull(deleteResult?.Data);
            Assert.False(deleteResult.Data.IsActive);
        }

        [Fact]
        public async Task GetLanguages_AdminOnly_ReturnsAll()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-getlang@test.com", "admin-getlang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/languages");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.Count >= 3);
        }

        [Fact]
        public async Task GetLanguages_WithIsActiveFilter_ReturnsFiltered()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-filter@test.com", "admin-filter", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/languages?isActive=true");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.All(result.Data, l => Assert.True(l.IsActive));
        }

        [Fact]
        public async Task GetActiveLanguages_IsAnonymous()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            // Không cần token
            var response = await client.GetAsync("/api/languages/active");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<LanguageDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.All(result.Data, l => Assert.True(l.IsActive));
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task CreateLanguage_DuplicateCode_Returns409()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-dup-lang@test.com", "admin-dup-lang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto { Name = "Korean", Code = "ko", IsActive = true });

            var response = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto { Name = "Korean2", Code = "ko", IsActive = true });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateLanguage_AsBusinessOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-lang@test.com", "bo-lang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto
            {
                Name = "Thai",
                Code = "th",
                IsActive = true
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLanguages_AsBusinessOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "bo-lang2@test.com", "bo-lang2", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/languages");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateLanguage_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-del-nf@test.com", "admin-del-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"/api/languages/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateLanguage_DuplicateCode_Returns409()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-upd-dup@test.com", "admin-upd-dup", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto { Name = "Lang A", Code = "la", IsActive = true });
            var respB = await client.PostAsJsonAsync("/api/languages", new LanguageCreateDto { Name = "Lang B", Code = "lb", IsActive = true });
            var langB = (await respB.Content.ReadFromJsonAsync<ApiResult<LanguageDetailDto>>(JsonOptions.Default))!.Data!;

            // Cố gắng đổi code của Lang B thành "la" (đã tồn tại)
            var response = await client.PutAsJsonAsync($"/api/languages/{langB.Id}", new LanguageUpdateDto
            {
                Name = "Lang B Updated",
                Code = "la",
                IsActive = true
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
