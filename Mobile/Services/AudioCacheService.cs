namespace Mobile.Services;

public interface IAudioCacheService
{
    /// <summary>Download audio nếu chưa có local. Trả về local file path, null nếu thất bại.</summary>
    Task<string?> EnsureDownloadedAsync(
        string audioUrl, string stallId, string languageCode,
        CancellationToken ct = default);

    /// <summary>Lấy path file local nếu đã tồn tại, null nếu chưa download.</summary>
    string? GetLocalPath(string stallId, string languageCode);

    /// <summary>Xóa audio cache của 1 ngôn ngữ (khi user đổi ngôn ngữ).</summary>
    Task DeleteByLanguageAsync(string languageCode);

    /// <summary>Xóa toàn bộ cache.</summary>
    Task ClearAllAsync();
}

public class AudioCacheService : IAudioCacheService
{
    // Path: {AppDataDirectory}/audio/{languageCode}/{stallId}.mp3
    private static string AudioRootDir =>
        Path.Combine(FileSystem.AppDataDirectory, "audio");

    private static string GetAudioDir(string languageCode) =>
        Path.Combine(AudioRootDir, languageCode);

    private static string GetFilePath(string stallId, string languageCode) =>
        Path.Combine(GetAudioDir(languageCode), $"{stallId}.mp3");

    public string? GetLocalPath(string stallId, string languageCode)
    {
        var path = GetFilePath(stallId, languageCode);
        return File.Exists(path) ? path : null;
    }

    public async Task<string?> EnsureDownloadedAsync(
        string audioUrl, string stallId, string languageCode,
        CancellationToken ct = default)
    {
        var path = GetFilePath(stallId, languageCode);
        if (File.Exists(path)) return path; // đã có, không download lại

        try
        {
            Directory.CreateDirectory(GetAudioDir(languageCode));

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var bytes = await client.GetByteArrayAsync(audioUrl, ct);
            await File.WriteAllBytesAsync(path, bytes, ct);
            return path;
        }
        catch
        {
            // Xóa file corrupt nếu download lỗi giữa chừng
            if (File.Exists(path)) File.Delete(path);
            return null;
        }
    }

    public Task DeleteByLanguageAsync(string languageCode)
    {
        var dir = GetAudioDir(languageCode);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync()
    {
        if (Directory.Exists(AudioRootDir))
            Directory.Delete(AudioRootDir, recursive: true);
        return Task.CompletedTask;
    }
}
