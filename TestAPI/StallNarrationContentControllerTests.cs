using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;
using Xunit;

namespace TestAPI
{
    /// <summary>
    /// TTS được mock bằng FakeNarrationAudioService (đăng ký trong ApiFactory).
    /// </summary>
    public class StallNarrationContentControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Create_NarrationContent_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-owner@test.com", "narc-owner", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc Stall", "narc-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-owner@test.com", "narc-owner", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId,
                LanguageId = languageId,
                Title = "Chào mừng đến Gian Hàng",
                Description = "Giới thiệu gian hàng",
                ScriptText = "Chào mừng bạn đến gian hàng của chúng tôi!",
                IsActive = true
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data?.Content);
            Assert.Equal(stallId, result.Data.Content.StallId);
            Assert.Equal("Chào mừng đến Gian Hàng", result.Data.Content.Title);
            Assert.True(result.Data.Content.IsActive);
        }

        [Fact]
        public async Task Create_NarrationContent_ActiveDeactivatesOthers()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-deact@test.com", "narc-deact", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc Deact Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc Deact Stall", "narc-deact-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-deact@test.com", "narc-deact", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Tạo content đầu tiên (active)
            await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Version 1", ScriptText = "Script 1", IsActive = true
            });

            // Tạo content thứ hai (cũng active) → content 1 phải bị deactivate
            var response2 = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Version 2", ScriptText = "Script 2", IsActive = true
            });

            response2.EnsureSuccessStatusCode();

            // Lấy danh sách, chỉ 1 content active
            var listResponse = await client.GetAsync($"/api/stall-narration-content?stallId={stallId}&isActive=true");
            listResponse.EnsureSuccessStatusCode();
            var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallNarrationContentDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(listResult?.Data);
            Assert.Equal(1, listResult.Data.TotalCount);
            Assert.Equal("Version 2", listResult.Data.Items[0].Title);
        }

        [Fact]
        public async Task Update_NarrationContent_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-upd@test.com", "narc-upd", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc Upd Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc Upd Stall", "narc-upd-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-upd@test.com", "narc-upd", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Old Title", ScriptText = "Old script", IsActive = false
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>(JsonOptions.Default))!.Data!.Content;

            var updateResponse = await client.PutAsJsonAsync($"/api/stall-narration-content/{created.Id}", new StallNarrationContentUpdateDto
            {
                Title = "New Title",
                Description = "Updated description",
                ScriptText = "New script text updated",
                IsActive = true
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("New Title", result.Data.Title);
            Assert.True(result.Data.IsActive);
        }

        [Fact]
        public async Task ToggleStatus_NarrationContent_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-toggle@test.com", "narc-toggle", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc Toggle Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc Toggle Stall", "narc-toggle-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-toggle@test.com", "narc-toggle", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Toggle Me", ScriptText = "Some script", IsActive = false
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>(JsonOptions.Default))!.Data!.Content;

            var toggleResponse = await client.PatchAsJsonAsync($"/api/stall-narration-content/{created.Id}/status", true);
            toggleResponse.EnsureSuccessStatusCode();
            var result = await toggleResponse.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.True(result.Data.IsActive);
        }

        [Fact]
        public async Task GetDetail_NarrationContent_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-det@test.com", "narc-det", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc Det Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc Det Stall", "narc-det-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-det@test.com", "narc-det", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createResponse = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Detail Content", ScriptText = "Detail script", IsActive = false
            });
            createResponse.EnsureSuccessStatusCode();
            var created = (await createResponse.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>(JsonOptions.Default))!.Data!.Content;

            var response = await client.GetAsync($"/api/stall-narration-content/{created.Id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<StallNarrationContentWithAudiosDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data?.Content);
            Assert.Equal(created.Id, result.Data.Content.Id);
            Assert.Equal("Detail Content", result.Data.Content.Title);
        }

        [Fact]
        public async Task GetList_NarrationContents_Success()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-list@test.com", "narc-list", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc List Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc List Stall", "narc-list-stall");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-list@test.com", "narc-list", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId, Title = "Content 1", ScriptText = "Script 1", IsActive = false
            });
            await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId, Title = "Content 2", ScriptText = "Script 2", IsActive = false
            });

            var response = await client.GetAsync($"/api/stall-narration-content?stallId={stallId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<PagedResult<StallNarrationContentDetailDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(2, result.Data.TotalCount);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task Create_NarrationContent_InvalidLanguage_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                var user = TestDataSeeder.SeedUserWithRole(context, "narc-badlang@test.com", "narc-badlang", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, user.Id, "Narc BadLang Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Narc BadLang Stall", "narc-badlang-stall");
                stallId = stall.Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-badlang@test.com", "narc-badlang", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId,
                LanguageId = Guid.NewGuid(), // không tồn tại
                Title = "Test", ScriptText = "Test script", IsActive = false
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_NarrationContent_OtherOwner_Returns403()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            Guid stallId, languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedRoles(context);
                TestDataSeeder.SeedLanguages(context);
                var owner = TestDataSeeder.SeedUserWithRole(context, "narc-own2@test.com", "narc-own2", "Pass@123", "BusinessOwner");
                var business = TestDataSeeder.SeedBusiness(context, owner.Id, "Protected Narc Biz");
                var stall = TestDataSeeder.SeedStall(context, business.Id, "Protected Narc Stall", "protected-narc");
                stallId = stall.Id;
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "BusinessOwner", "narc-att@test.com", "narc-att", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = stallId, LanguageId = languageId,
                Title = "Hack", ScriptText = "Hack script", IsActive = false
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Create_NarrationContent_WithoutToken_Returns401()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/stall-narration-content", new StallNarrationContentCreateDto
            {
                StallId = Guid.NewGuid(),
                LanguageId = Guid.NewGuid(),
                Title = "Unauthorized", ScriptText = "Script"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetDetail_NarrationContent_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var (token, _) = await TestAuthHelper.LoginAsync(factory, client, "Admin", "admin-narc-nf@test.com", "admin-narc-nf", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/stall-narration-content/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
