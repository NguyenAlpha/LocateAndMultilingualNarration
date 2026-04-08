namespace Mobile.Services;

/// <summary>
/// Quản lý bộ nhớ đệm file âm thanh đã tải về máy để phát offline theo từng ngôn ngữ và gian hàng.
/// </summary>
public interface IAudioCacheService
{
    /// <summary>
    /// Tải audio về máy nếu chưa có bản local.
    /// </summary>
    /// <param name="audioUrl">URL nguồn của file audio.</param>
    /// <param name="stallId">Mã gian hàng dùng để đặt tên file.</param>
    /// <param name="languageCode">Mã ngôn ngữ dùng để phân vùng cache.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Đường dẫn file local nếu tải thành công; ngược lại trả về <c>null</c>.</returns>
    Task<string?> EnsureDownloadedAsync(
        string audioUrl, string stallId, string languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy đường dẫn file local nếu file đã tồn tại trong cache.
    /// </summary>
    /// <param name="stallId">Mã gian hàng.</param>
    /// <param name="languageCode">Mã ngôn ngữ.</param>
    /// <returns>Đường dẫn file local nếu có; ngược lại <c>null</c>.</returns>
    string? GetLocalPath(string stallId, string languageCode);

    /// <summary>
    /// Xóa toàn bộ cache audio của một ngôn ngữ.
    /// </summary>
    /// <param name="languageCode">Mã ngôn ngữ cần xóa cache.</param>
    /// <returns>Task đại diện cho thao tác xóa.</returns>
    Task DeleteByLanguageAsync(string languageCode);

    /// <summary>
    /// Xóa toàn bộ cache audio đã lưu trong máy.
    /// </summary>
    /// <returns>Task đại diện cho thao tác xóa.</returns>
    Task ClearAllAsync();
}

/// <summary>
/// Triển khai lưu và truy xuất file audio cục bộ theo cấu trúc thư mục riêng cho từng ngôn ngữ.
/// </summary>
public class AudioCacheService : IAudioCacheService
{
    // Path: {AppDataDirectory}/audio/{languageCode}/{stallId}.mp3
    private static string AudioRootDir =>
        Path.Combine(FileSystem.AppDataDirectory, "audio");

    private static string GetAudioDir(string languageCode) =>
        Path.Combine(AudioRootDir, languageCode);

    private static string GetFilePath(string stallId, string languageCode) =>
        Path.Combine(GetAudioDir(languageCode), $"{stallId}.mp3");

    /// <summary>
    /// Lấy đường dẫn file local nếu file đã tồn tại trong cache.
    /// </summary>
    /// <param name="stallId">Mã gian hàng.</param>
    /// <param name="languageCode">Mã ngôn ngữ.</param>
    /// <returns>Đường dẫn file local nếu có; ngược lại <c>null</c>.</returns>
    public string? GetLocalPath(string stallId, string languageCode)
    {
        // Tạo lại đường dẫn chuẩn của file cache tương ứng.
        var path = GetFilePath(stallId, languageCode);
        // Chỉ trả về khi file thật sự tồn tại.
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Tải audio về máy nếu chưa có bản local.
    /// </summary>
    /// <param name="audioUrl">URL nguồn của file audio.</param>
    /// <param name="stallId">Mã gian hàng dùng để đặt tên file.</param>
    /// <param name="languageCode">Mã ngôn ngữ dùng để phân vùng cache.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Đường dẫn file local nếu tải thành công; ngược lại trả về <c>null</c>.</returns>
    public async Task<string?> EnsureDownloadedAsync(
        string audioUrl, string stallId, string languageCode,
        CancellationToken ct = default)
    {
        var path = GetFilePath(stallId, languageCode);
        // Nếu đã có file thì không tải lại.
        if (File.Exists(path)) return path;

        try
        {
            // Đảm bảo thư mục theo ngôn ngữ đã tồn tại trước khi ghi file.
            Directory.CreateDirectory(GetAudioDir(languageCode));

            // Tạo HttpClient tạm thời để tải file audio từ URL.
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            // Tải dữ liệu nhị phân của file audio.
            var bytes = await client.GetByteArrayAsync(audioUrl, ct);
            // Ghi toàn bộ bytes xuống file local.
            await File.WriteAllBytesAsync(path, bytes, ct);
            return path;
        }
        catch
        {
            // Xóa file corrupt nếu download lỗi giữa chừng.
            if (File.Exists(path)) File.Delete(path);
            return null;
        }
    }

    /// <summary>
    /// Xóa toàn bộ cache audio của một ngôn ngữ.
    /// </summary>
    /// <param name="languageCode">Mã ngôn ngữ cần xóa cache.</param>
    /// <returns>Task đại diện cho thao tác xóa.</returns>
    public Task DeleteByLanguageAsync(string languageCode)
    {
        // Xác định thư mục cache của ngôn ngữ cần xóa.
        var dir = GetAudioDir(languageCode);
        // Nếu thư mục tồn tại thì xóa toàn bộ nội dung bên trong.
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Xóa toàn bộ cache audio đã lưu trong máy.
    /// </summary>
    /// <returns>Task đại diện cho thao tác xóa.</returns>
    public Task ClearAllAsync()
    {
        // Xóa toàn bộ thư mục gốc của cache audio nếu tồn tại.
        if (Directory.Exists(AudioRootDir))
            Directory.Delete(AudioRootDir, recursive: true);
        return Task.CompletedTask;
    }
}
