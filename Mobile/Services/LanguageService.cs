using System.Text.Json;
using Shared.DTOs.Languages;

namespace Mobile.Services;

/// <summary>
/// Cung cấp dữ liệu ngôn ngữ và đồng bộ lựa chọn ngôn ngữ của người dùng lên API.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Lấy danh sách ngôn ngữ đang hoạt động.
    /// </summary>
    /// <param name="forceRefresh">Giá trị <c>true</c> để bỏ qua cache và tải lại từ API.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Danh sách ngôn ngữ.</returns>
    Task<IReadOnlyList<LanguageDetailDto>> GetLanguagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

}

/// <summary>
/// Triển khai logic lấy danh sách ngôn ngữ và cập nhật lựa chọn ngôn ngữ của người dùng.
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private List<LanguageDetailDto>? _cachedLanguages;
    private DateTime _lastFetchUtc;

    private const string BaseUrl = "http://10.0.2.2:5299";

    public LanguageService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Lấy danh sách ngôn ngữ đang hoạt động, ưu tiên dùng cache trong bộ nhớ nếu còn hạn.
    /// </summary>
    /// <param name="forceRefresh">Giá trị <c>true</c> để bỏ qua cache và tải lại từ API.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Danh sách ngôn ngữ đang hoạt động.</returns>
    public async Task<IReadOnlyList<LanguageDetailDto>> GetLanguagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Cache để tránh gọi API lặp lại gây lag UI.
        // Nếu cache còn hiệu lực thì trả về ngay, không cần gọi mạng.
        if (!forceRefresh && _cachedLanguages is { Count: > 0 } && DateTime.UtcNow - _lastFetchUtc < CacheDuration)
        {
            return _cachedLanguages;
        }

        // Không có mạng và không có cache → báo cho UI biết để hiển thị thông báo.
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            throw new InvalidOperationException("no_network");

        try
        {
            // Tạo HttpClient từ factory để gọi API ngôn ngữ.
            var client = _httpClientFactory.CreateClient();
            // Gọi endpoint lấy danh sách ngôn ngữ đang active.
            var response = await client.GetAsync($"{BaseUrl}/api/languages/active", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // Nếu API lỗi thì trả lại dữ liệu cache hiện có, hoặc danh sách rỗng.
                return _cachedLanguages ?? [];
            }

            // Đọc dữ liệu JSON trả về từ API.
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            // Chuyển JSON thành danh sách DTO ngôn ngữ.
            var languages = ParseLanguages(raw);

            // Lưu lại cache trong bộ nhớ để dùng cho lần gọi tiếp theo.
            _cachedLanguages = languages;
            _lastFetchUtc = DateTime.UtcNow;
            return languages;
        }
        catch
        {
            // Nếu có lỗi bất kỳ thì ưu tiên trả cache hiện có để UI vẫn hoạt động.
            return _cachedLanguages ?? [];
        }
    }

    /// <summary>
    /// Phân tích JSON trả về từ API thành danh sách <see cref="LanguageDetailDto"/>.
    /// </summary>
    /// <param name="json">Chuỗi JSON cần phân tích.</param>
    /// <returns>Danh sách DTO ngôn ngữ đã parse.</returns>
    private static List<LanguageDetailDto> ParseLanguages(string json)
    {
        // Khởi tạo danh sách kết quả rỗng.
        var result = new List<LanguageDetailDto>();
        // Phân tích chuỗi JSON thành cây tài liệu để đọc linh hoạt cả object lẫn array.
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var list = root;

        // Nếu API bọc dữ liệu trong thuộc tính data thì lấy node data.
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataNode))
        {
            list = dataNode;
        }

        // Nếu không phải mảng thì không có dữ liệu hợp lệ để đọc.
        if (list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        // Duyệt từng phần tử JSON và map sang DTO.
        foreach (var item in list.EnumerateArray())
        {
            result.Add(new LanguageDetailDto
            {
                Id          = item.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : Guid.Empty,
                Name        = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Code        = item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty,
                DisplayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() : null,
                FlagCode    = item.TryGetProperty("flagCode", out var fc) ? fc.GetString() : null,
                IsActive    = item.TryGetProperty("isActive", out var ia) && ia.GetBoolean()
            });
        }

        return result;
    }
}
