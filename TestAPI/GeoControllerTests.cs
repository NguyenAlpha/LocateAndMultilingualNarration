using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Common;
using Shared.DTOs.Geo;
using Xunit;

namespace TestAPI
{
    public class GeoControllerTests
    {
        // ===================== HAPPY PATH =====================

        [Fact]
        public async Task GetAllStalls_Anonymous_ReturnsSuccess()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            // Không cần token – endpoint là AllowAnonymous
            var response = await client.GetAsync("/api/geo/stalls");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<GeoStallDto>>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
        }

        [Fact]
        public async Task GetAllStalls_WithDeviceId_ReturnsSuccess()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/stalls?deviceId=test-device-123");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<GeoStallDto>>>(JsonOptions.Default);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetNearestStall_ValidCoords_NoStallsInDb_ReturnsNoContent()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            // DB rỗng nên không tìm thấy stall nào
            var response = await client.GetAsync("/api/geo/nearest-stall?lat=10.762622&lng=106.660172");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // ===================== ERROR CASES =====================

        [Fact]
        public async Task GetNearestStall_InvalidLatitude_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/nearest-stall?lat=91&lng=106.660172");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNearestStall_InvalidLatitudeNegative_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/nearest-stall?lat=-91&lng=106.660172");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNearestStall_InvalidLongitude_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/nearest-stall?lat=10.76&lng=181");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNearestStall_InvalidRadius_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/nearest-stall?lat=10.76&lng=106.66&radius=0");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetNearestStall_NegativeRadius_Returns400()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/geo/nearest-stall?lat=10.76&lng=106.66&radius=-100");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
