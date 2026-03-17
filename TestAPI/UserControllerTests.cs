using System.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Users;
using Xunit;

namespace TestAPI
{
    public class UserControllerTests
    {
        [Fact]
        public async Task Get_User_Detail_Returns_Self()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("X-TimeZoneId", "SE Asia Standard Time");

            var (token, userId) = await TestAuthHelper.LoginAsync(factory, client, "User", "user@test.com", "user", "Pass@123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/User/{userId}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<UserDetailDto>>(JsonOptions.Default);
            Assert.NotNull(result?.Data);
            Assert.Equal(userId, result.Data.Id);
        }
    }
}
