using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Shared.DTOs.Auth;
using Shared.DTOs.Common;

namespace Web.Services
{
    public class ApiClient
    {
        public const string TokenSessionKey = "AuthToken";
        public const string TokenExpiresAtSessionKey = "AuthTokenExpiresAt";
        public const string RefreshTokenSessionKey = "RefreshToken";
        public const string RefreshTokenExpiresAtSessionKey = "RefreshTokenExpiresAt";
        public const string UserNameSessionKey = "UserName";
        public const string UserRoleSessionKey = "UserRole";

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResult<LoginResponseDto>?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<LoginResponseDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<RegisterResponseDto>?> RegisterBusinessOwnerAsync(RegisterBusinessOwnerDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register/business-owner", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<RegisterResponseDto>>(cancellationToken: cancellationToken);
        }

        public void StoreToken(LoginResponseDto response)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return;
            }

            session.SetString(TokenSessionKey, response.Token);
            session.SetString(TokenExpiresAtSessionKey, response.ExpiresAt.ToString("O"));
            session.SetString(RefreshTokenSessionKey, response.RefreshToken);
            session.SetString(RefreshTokenExpiresAtSessionKey, response.RefreshTokenExpiresAt.ToString("O"));
            session.SetString(UserNameSessionKey, response.UserName ?? string.Empty);
            session.SetString(UserRoleSessionKey, response.Roles.FirstOrDefault() ?? string.Empty);
        }

        public void ClearToken()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return;
            }

            session.Remove(TokenSessionKey);
            session.Remove(TokenExpiresAtSessionKey);
            session.Remove(RefreshTokenSessionKey);
            session.Remove(RefreshTokenExpiresAtSessionKey);
            session.Remove(UserNameSessionKey);
            session.Remove(UserRoleSessionKey);
        }

        public string? GetRefreshToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(RefreshTokenSessionKey);
        }

        public DateTimeOffset? GetTokenExpiresAt()
        {
            var value = _httpContextAccessor.HttpContext?.Session.GetString(TokenExpiresAtSessionKey);
            return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
        }
    }
}
