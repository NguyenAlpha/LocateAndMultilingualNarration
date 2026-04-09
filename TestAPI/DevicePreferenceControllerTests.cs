using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.DevicePreferences;
using Xunit;

namespace TestAPI
{
    public class DevicePreferenceControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task Upsert_CreatesNewPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            Guid viLanguageId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
                viLanguageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            // AllowAnonymous – không cần token
            var response = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-001",
                LanguageId = viLanguageId,
                SpeechRate = 1.0m,
                AutoPlay = true,
                Platform = "Android",
                DeviceModel = "Pixel 7",
                Manufacturer = "Google",
                OsVersion = "14"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("device-001", result.Data.DeviceId);
            Assert.Equal("vi", result.Data.LanguageCode);
            Assert.Null(result.Data.VoiceId);
        }

        [Fact]
        public async Task Upsert_UpdatesExistingPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            Guid viLanguageId;
            Guid enLanguageId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
                viLanguageId = context.Languages.First(l => l.Code == "vi").Id;
                enLanguageId = context.Languages.First(l => l.Code == "en").Id;
            }

            // Tạo lần đầu
            await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-002",
                LanguageId = viLanguageId,
                SpeechRate = 1.0m
            });

            // Cập nhật sang ngôn ngữ khác
            var updateResponse = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-002",
                LanguageId = enLanguageId,
                SpeechRate = 1.2m,
                AutoPlay = false
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("en", result.Data.LanguageCode);
            Assert.Null(result.Data.VoiceId);
        }

        [Fact]
        public async Task GetByDeviceId_ReturnsPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            Guid viLanguageId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
                viLanguageId = context.Languages.First(l => l.Code == "vi").Id;
            }

            await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-get-001",
                LanguageId = viLanguageId,
                SpeechRate = 1.0m
            });

            var response = await client.GetAsync("/api/device-preference/device-get-001");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("device-get-001", result.Data.DeviceId);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetByDeviceId_NotFound_Returns404()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/device-preference/non-existent-device");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Upsert_InvalidLanguageCode_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            var response = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-bad-lang",
                LanguageId = Guid.NewGuid(), // không tồn tại
                SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upsert_InactiveLanguageCode_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();
            Guid jaLanguageId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context); // "ja" là inactive
                jaLanguageId = context.Languages.First(l => l.Code == "ja").Id;
            }

            var response = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-inactive-lang",
                LanguageId = jaLanguageId, // inactive trong seed data
                SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
