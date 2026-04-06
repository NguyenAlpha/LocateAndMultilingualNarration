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

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            // AllowAnonymous – không cần token
            var response = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-001",
                LanguageCode = "vi",
                Voice = "vi-VN-Standard-A",
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
            Assert.Equal("vi-VN-Standard-A", result.Data.Voice);
        }

        [Fact]
        public async Task Upsert_UpdatesExistingPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            // Tạo lần đầu
            await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-002",
                LanguageCode = "vi",
                SpeechRate = 1.0m
            });

            // Cập nhật sang ngôn ngữ khác
            var updateResponse = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-002",
                LanguageCode = "en",
                Voice = "en-US-Standard-B",
                SpeechRate = 1.2m,
                AutoPlay = false
            });

            updateResponse.EnsureSuccessStatusCode();
            var result = await updateResponse.Content.ReadFromJsonAsync<ApiResult<DevicePreferenceDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal("en", result.Data.LanguageCode);
            Assert.Equal("en-US-Standard-B", result.Data.Voice);
        }

        [Fact]
        public async Task GetByDeviceId_ReturnsPreference()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context);
            }

            await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-get-001",
                LanguageCode = "vi",
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
                LanguageCode = "zz", // không tồn tại
                SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upsert_InactiveLanguageCode_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                TestDataSeeder.SeedLanguages(context); // "ja" là inactive
            }

            var response = await client.PostAsJsonAsync("/api/device-preference", new DevicePreferenceUpsertDto
            {
                DeviceId = "device-inactive-lang",
                LanguageCode = "ja", // inactive trong seed data
                SpeechRate = 1.0m
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
