using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.TtsVoiceProfiles;
using Xunit;

namespace TestAPI
{
    public class TtsVoiceProfileControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task GetActive_WithLanguageId_ReturnsEmptyWhenNoProfiles()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            Guid languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                languageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            // AllowAnonymous – không cần token
            var response = await client.GetAsync($"/api/tts-voice-profiles/active?languageId={languageId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<TtsVoiceProfileListItemDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            // DB rỗng (chưa seed profile) → trả về danh sách rỗng
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetActive_WithSeededProfiles_ReturnsProfiles()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            Guid languageId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
                languageId = context.Languages.First(l => l.Code == "vi").Id;
                TestDataSeeder.SeedTtsVoiceProfile(context, languageId, "Nam Miền Bắc");
                TestDataSeeder.SeedTtsVoiceProfile(context, languageId, "Nữ Miền Nam");
            }

            var response = await client.GetAsync($"/api/tts-voice-profiles/active?languageId={languageId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<TtsVoiceProfileListItemDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task GetActive_ProfilesForDifferentLanguage_ReturnsEmpty()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            Guid viId, enId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
                viId = context.Languages.First(l => l.Code == "vi").Id;
                enId = context.Languages.First(l => l.Code == "en").Id;
                TestDataSeeder.SeedTtsVoiceProfile(context, viId, "Vi Profile");
            }

            // Lấy profile của tiếng Anh – không có → rỗng
            var response = await client.GetAsync($"/api/tts-voice-profiles/active?languageId={enId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<TtsVoiceProfileListItemDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Empty(result.Data);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetActive_WithoutLanguageId_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/tts-voice-profiles/active");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
