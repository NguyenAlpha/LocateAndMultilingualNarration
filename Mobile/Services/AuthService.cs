using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mobile.Services;

public interface IAuthService
{
    Task<(bool IsSuccess, string ErrorMessage, string Token, string UserName)> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string BaseUrl = "http://10.0.2.2:5299";

    public AuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool IsSuccess, string ErrorMessage, string Token, string UserName)> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{BaseUrl}/api/auth/login", new { email, password }, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Đăng nhập thất bại ({(int)response.StatusCode})", string.Empty, string.Empty);
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var data = root;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataNode))
            {
                data = dataNode;
            }

            var token = data.TryGetProperty("token", out var tokenNode) ? tokenNode.GetString() ?? string.Empty : string.Empty;
            var userName = data.TryGetProperty("userName", out var userNameNode) ? userNameNode.GetString() ?? "Guest" : "Guest";

            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "Không lấy được token từ API.", string.Empty, string.Empty);
            }

            return (true, string.Empty, token, userName);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, string.Empty, string.Empty);
        }
    }
}
