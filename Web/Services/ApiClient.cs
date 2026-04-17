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
        public const string UserPlanSessionKey = "UserPlan";
        public const string UserPlanExpiresAtSessionKey = "UserPlanExpiresAt";

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
            session.SetString(UserRoleSessionKey, response.Roles?.FirstOrDefault() ?? string.Empty);
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
            session.Remove(UserPlanSessionKey);
            session.Remove(UserPlanExpiresAtSessionKey);
        }

        public void StoreUserPlan(string plan, DateTimeOffset? planExpiresAt)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;
            session.SetString(UserPlanSessionKey, plan);
            session.SetString(UserPlanExpiresAtSessionKey, planExpiresAt?.ToString("O") ?? string.Empty);
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

        public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return false;

            var refreshToken = session.GetString(RefreshTokenSessionKey);
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/refresh",
                    new RefreshTokenRequestDto { RefreshToken = refreshToken }, cancellationToken);

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<ApiResult<RefreshResponseDto>>(cancellationToken: cancellationToken);
                if (result?.Success != true || result.Data == null) return false;

                session.SetString(TokenSessionKey, result.Data.Token);
                session.SetString(TokenExpiresAtSessionKey, result.Data.ExpiresAt.ToString("O"));
                session.SetString(RefreshTokenSessionKey, result.Data.RefreshToken);
                session.SetString(RefreshTokenExpiresAtSessionKey, result.Data.RefreshTokenExpiresAt.ToString("O"));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
