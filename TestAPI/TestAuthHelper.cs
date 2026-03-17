using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.Common;

namespace TestAPI
{
    public static class TestAuthHelper
    {
        public static async Task<(string Token, Guid UserId)> LoginAsync(ApiFactory factory, HttpClient client, string roleName, string email, string userName, string password)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            TestDataSeeder.SeedRoles(context);
            var user = TestDataSeeder.SeedUserWithRole(context, email, userName, password, roleName);

            var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto
            {
                Email = email,
                Password = password
            });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<LoginResponseDto>>(JsonOptions.Default);
            if (result?.Data == null)
            {
                throw new InvalidOperationException("Login không trả về token.");
            }

            return (result.Data.Token, user.Id);
        }
    }
}
