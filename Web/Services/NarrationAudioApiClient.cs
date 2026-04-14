using System.Net;
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

        public async Task<ApiResult<NarrationAudioDetailDto>?> UploadHumanAudioAsync(
            Guid audioId, IFormFile audioFile, CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            await using var stream = audioFile.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(audioFile.ContentType);
            content.Add(fileContent, "audioFile", audioFile.FileName);

            var response = await _httpClient.PutAsync($"api/narration-audio/{audioId}/upload", content, cancellationToken);
            return await ReadApiResultAsync<NarrationAudioDetailDto>(response, "Cập nhật audio thất bại.", cancellationToken);
        }

        private static async Task<ApiResult<T>?> ReadApiResultAsync<T>(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
        {
            try
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResult<T>>(cancellationToken: cancellationToken);
                if (result != null)
                    return result;
            }
            catch (NotSupportedException) { }
            catch (System.Text.Json.JsonException) { }

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
