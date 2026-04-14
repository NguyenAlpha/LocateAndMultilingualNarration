using Shared.DTOs.Languages;

namespace Mobile.Services;

public interface ILanguageService
{
    /// <summary>
    /// Lấy danh sách ngôn ngữ đang active từ API.
    /// </summary>
    Task<IReadOnlyList<LanguageDetailDto>> GetActiveLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả ngôn ngữ (có thể dùng cho admin hoặc cache).
    /// </summary>
    Task<IReadOnlyList<LanguageDetailDto>> GetLanguagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}