using System.Text.Json;
using Shared.DTOs.TtsVoiceProfiles;

namespace Mobile.Services;

/// <summary>
/// Cung cấp danh sách giọng đọc theo ngôn ngữ để màn hình lựa chọn voice sử dụng.
/// </summary>
public interface IVoiceService
{
    /// <summary>
    /// Lấy danh sách giọng đọc đang hoạt động theo ngôn ngữ.
    /// </summary>
    /// <param name="languageId">Mã ngôn ngữ cần lấy voice.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Danh sách voice profile phù hợp với ngôn ngữ.</returns>
    Task<IReadOnlyList<TtsVoiceProfileListItemDto>> GetVoicesByLanguageAsync(Guid languageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Triển khai gọi API để lấy danh sách giọng đọc theo ngôn ngữ.
/// </summary>
public class VoiceService : IVoiceService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string BaseUrl = "http://10.0.2.2:5299";

    /// <summary>
    /// Khởi tạo service với factory tạo HttpClient.
    /// </summary>
    /// <param name="httpClientFactory">Factory dùng để tạo HttpClient.</param>
    public VoiceService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Lấy danh sách giọng đọc đang hoạt động theo ngôn ngữ.
    /// </summary>
    /// <param name="languageId">Mã ngôn ngữ cần lấy voice.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Danh sách voice profile phù hợp với ngôn ngữ; nếu lỗi thì trả về danh sách rỗng.</returns>
    public async Task<IReadOnlyList<TtsVoiceProfileListItemDto>> GetVoicesByLanguageAsync(Guid languageId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tạo client để gọi endpoint voice theo ngôn ngữ.
            var client = _httpClientFactory.CreateClient();
            // Gọi API lấy danh sách voice đang active.
            var response = await client.GetAsync($"{BaseUrl}/api/tts-voice-profiles/active?languageId={languageId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            // Đọc chuỗi JSON trả về từ API.
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            // Parse JSON thành danh sách DTO.
            return ParseVoices(raw);
        }
        catch
        {
            // Nếu có lỗi thì trả về danh sách rỗng để UI không bị gián đoạn.
            return [];
        }
    }

    /// <summary>
    /// Phân tích JSON trả về từ API thành danh sách voice profile.
    /// </summary>
    /// <param name="json">Chuỗi JSON cần phân tích.</param>
    /// <returns>Danh sách voice profile đã parse.</returns>
    private static List<TtsVoiceProfileListItemDto> ParseVoices(string json)
    {
        // Khởi tạo danh sách kết quả.
        var result = new List<TtsVoiceProfileListItemDto>();
        // Phân tích JSON thành cây tài liệu để đọc linh hoạt cả object lẫn array.
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var list = root;

        // Nếu dữ liệu được bọc trong thuộc tính data thì lấy node data.
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataNode))
            list = dataNode;

        // Không phải mảng thì không có dữ liệu hợp lệ.
        if (list.ValueKind != JsonValueKind.Array)
            return result;

        // Duyệt từng item JSON và map sang DTO.
        foreach (var item in list.EnumerateArray())
        {
            result.Add(new TtsVoiceProfileListItemDto
            {
                Id          = item.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : Guid.Empty,
                LanguageId  = item.TryGetProperty("languageId", out var lid) && lid.TryGetGuid(out var lg) ? lg : Guid.Empty,
                DisplayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty,
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Style       = item.TryGetProperty("style", out var st) ? st.GetString() : null,
                Role        = item.TryGetProperty("role", out var role) ? role.GetString() : null,
                IsDefault   = item.TryGetProperty("isDefault", out var isd) && isd.GetBoolean(),
                Priority    = item.TryGetProperty("priority", out var pri) ? pri.GetInt32() : 0
            });
        }

        return result;
    }
}
