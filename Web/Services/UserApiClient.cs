using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Users;

namespace Web.Services
{
    public class UserApiClient
    {
        private readonly HttpClient _httpClient;

        public UserApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<UserListItemDto>>?> GetUsersAsync(
            int page, int pageSize,
            string? search, string? role, bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var url = $"api/user?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(role))
                url += $"&role={Uri.EscapeDataString(role)}";
            if (isActive.HasValue)
                url += $"&isActive={isActive.Value.ToString().ToLower()}";

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<UserListItemDto>>>(url, cancellationToken);
        }

        public async Task<ApiResult<UserListItemDto>?> AdminCreateUserAsync(
            AdminCreateUserDto dto,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("api/user", dto, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<UserListItemDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<object>?> ToggleUserActiveAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/user/{userId}/toggle-active", new { }, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<object>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<UserListItemDto>?> UpdateUserRoleAsync(
            Guid userId, UserRoleUpdateDto dto,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/user/{userId}/role", dto, cancellationToken);
            return await response.Content.ReadFromJsonAsync<ApiResult<UserListItemDto>>(cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<List<RoleListItemDto>>?> GetRolesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetFromJsonAsync<ApiResult<List<RoleListItemDto>>>("api/user/roles", cancellationToken);
        }
    }
}
