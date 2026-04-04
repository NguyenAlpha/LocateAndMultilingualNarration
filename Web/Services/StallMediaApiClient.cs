using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Shared.DTOs.Common;
using Shared.DTOs.StallMedia;

namespace Web.Services
{
    public class StallMediaApiClient
    {
        private readonly HttpClient _httpClient;

        public StallMediaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PagedResult<StallMediaDetailDto>>?> GetListAsync(
            int page, int pageSize, Guid? stallId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var url = $"api/stall-media?page={page}&pageSize={pageSize}";
            if (stallId.HasValue) url += $"&stallId={stallId.Value}";
            if (isActive.HasValue) url += $"&isActive={isActive.Value}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await ReadApiResultAsync<PagedResult<StallMediaDetailDto>>(response, "Không lấy được danh sách media.", cancellationToken);
        }

        public async Task<ApiResult<StallMediaDetailDto>?> UploadCreateAsync(
            Guid stallId, IFormFile imageFile, string? caption, int sortOrder, bool isActive,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(stallId.ToString()), "stallId");
            await using var stream = imageFile.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(fileContent, "imageFile", imageFile.FileName);
            if (!string.IsNullOrEmpty(caption))
                content.Add(new StringContent(caption), "caption");
            content.Add(new StringContent(sortOrder.ToString()), "sortOrder");
            content.Add(new StringContent(isActive.ToString().ToLower()), "isActive");

            var response = await _httpClient.PostAsync("api/stall-media/upload", content, cancellationToken);
            return await ReadApiResultAsync<StallMediaDetailDto>(response, "Tạo media thất bại.", cancellationToken);
        }

        public async Task<ApiResult<StallMediaDetailDto>?> UploadUpdateAsync(
            Guid id, IFormFile imageFile, string? caption, int sortOrder, bool isActive,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            await using var stream = imageFile.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(fileContent, "imageFile", imageFile.FileName);
            if (!string.IsNullOrEmpty(caption))
                content.Add(new StringContent(caption), "caption");
            content.Add(new StringContent(sortOrder.ToString()), "sortOrder");
            content.Add(new StringContent(isActive.ToString().ToLower()), "isActive");

            var response = await _httpClient.PutAsync($"api/stall-media/{id}/upload", content, cancellationToken);
            return await ReadApiResultAsync<StallMediaDetailDto>(response, "Cập nhật media thất bại.", cancellationToken);
        }

        public async Task<ApiResult<bool>?> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.DeleteAsync($"api/stall-media/{id}", cancellationToken);
            return await ReadApiResultAsync<bool>(response, "Xóa media thất bại.", cancellationToken);
        }

        private static async Task<ApiResult<T>?> ReadApiResultAsync<T>(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
        {
            try
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(cancellationToken: cancellationToken);
                if (result != null)
                    return result;
            }
            catch (NotSupportedException)
            {
            }
            catch (System.Text.Json.JsonException)
            {
            }

            if (response.IsSuccessStatusCode)
            {
                return ApiResult<T>.FromError(new ErrorDetail
                {
                    Code = ErrorCode.ServerError,
                    Message = fallbackMessage
                });
            }

            var message = response.ReasonPhrase ?? fallbackMessage;
            return ApiResult<T>.FromError(new ErrorDetail
            {
                Code = MapStatusCode(response.StatusCode),
                Message = message
            });
        }

        private static ErrorCode MapStatusCode(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest => ErrorCode.Validation,
                HttpStatusCode.Unauthorized => ErrorCode.Unauthorized,
                HttpStatusCode.Forbidden => ErrorCode.Forbidden,
                HttpStatusCode.NotFound => ErrorCode.NotFound,
                HttpStatusCode.Conflict => ErrorCode.Conflict,
                _ => ErrorCode.ServerError
            };
        }
    }
}
