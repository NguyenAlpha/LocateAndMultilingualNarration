using System.Net.Http.Json;
using Shared.DTOs.Common;
using Shared.DTOs.Narrations;

namespace Web.Services
{
    public class NarrationAudioApiClient
    {
        private readonly HttpClient _httpClient;

        public NarrationAudioApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<NarrationAudioDetailDto>>?> GetAudiosAsync(int page, int pageSize, Guid? narrationContentId, Guid? stallId, CancellationToken cancellationToken = default)
        {
            var url = $"api/narration-audio?page={page}&pageSize={pageSize}";

            if (narrationContentId.HasValue)
            {
                url += $"&narrationContentId={narrationContentId.Value}";
            }

            if (stallId.HasValue)
            {
                url += $"&stallId={stallId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<ApiResult<PagedResult<NarrationAudioDetailDto>>>(url, cancellationToken);
        }
    }
}
