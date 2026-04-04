using Plugin.Maui.Audio;

namespace Mobile.Services;

/// <summary>
/// Cung cấp chức năng phát, tạm dừng, tiếp tục và dừng âm thanh hướng dẫn.
/// </summary>
public interface IAudioGuideService
{
    /// <summary>
    /// Cho biết audio hiện tại có đang phát hay không.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// URL hoặc đường dẫn của audio đang được phát.
    /// </summary>
    string? CurrentUrl { get; }

    /// <summary>
    /// Phát audio từ URL hoặc đường dẫn local.
    /// </summary>
    /// <param name="url">URL mạng hoặc đường dẫn file local.</param>
    Task PlayAsync(string url);

    /// <summary>
    /// Tạm dừng audio hiện tại.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Tiếp tục phát audio đã tạm dừng.
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    /// Dừng audio hiện tại và giải phóng tài nguyên.
    /// </summary>
    Task StopAsync();
}

/// <summary>
/// Triển khai logic phát audio bằng <see cref="Plugin.Maui.Audio"/>.
/// </summary>
public class AudioGuideService : IAudioGuideService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    private MemoryStream? _buffer;

    /// <summary>
    /// Cho biết player hiện tại có đang phát hay không.
    /// </summary>
    public bool IsPlaying => _player?.IsPlaying ?? false;

    /// <summary>
    /// URL hoặc đường dẫn của audio đang được phát.
    /// </summary>
    public string? CurrentUrl { get; private set; }

    /// <summary>
    /// Khởi tạo service với audio manager của plugin.
    /// </summary>
    /// <param name="audioManager">Audio manager dùng để tạo player.</param>
    public AudioGuideService(IAudioManager audioManager)
        => _audioManager = audioManager;

    /// <summary>
    /// Phát audio từ đường dẫn local hoặc từ URL mạng.
    /// </summary>
    /// <param name="url">URL mạng hoặc đường dẫn file local.</param>
    public async Task PlayAsync(string url)
    {
        // Bỏ qua nếu không có giá trị hợp lệ.
        if (string.IsNullOrWhiteSpace(url)) return;

        // Dừng audio cũ trước khi phát nội dung mới.
        await StopAsync(); // dừng audio cũ nếu có
        CurrentUrl = url;

        Stream? stream;

        // Local file path → đọc từ disk, không cần mạng
        // Remote URL → stream từ network
        // Chọn nguồn stream dựa vào việc url là file local hay URL mạng.
        if (File.Exists(url))
            stream = File.OpenRead(url);
        else
            stream = await GetStreamFromUrlAsync(url);

        // Nếu không lấy được stream thì kết thúc và xóa trạng thái hiện tại.
        if (stream is null)
        {
            CurrentUrl = null;
            return;
        }

        // Copy toàn bộ stream vào bộ nhớ để player có thể phát ổn định.
        _buffer = new MemoryStream();
        await stream.CopyToAsync(_buffer);
        await stream.DisposeAsync();
        _buffer.Position = 0;

        // Tạo player từ buffer trong bộ nhớ và phát ngay.
        _player = _audioManager.CreatePlayer(_buffer);
        _player.Play();
    }

    /// <summary>
    /// Tạm dừng audio hiện tại.
    /// </summary>
    public Task PauseAsync()
    {
        // Chỉ gọi Pause nếu player đang tồn tại.
        _player?.Pause();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tiếp tục phát audio đã tạm dừng.
    /// </summary>
    public Task ResumeAsync()
    {
        // Chỉ phát tiếp khi player tồn tại và đang ở trạng thái dừng/tạm dừng.
        if (_player is { IsPlaying: false })
            _player.Play();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dừng audio hiện tại và giải phóng bộ nhớ.
    /// </summary>
    public Task StopAsync()
    {
        // Dừng và giải phóng player hiện tại nếu có.
        _player?.Stop();
        _player?.Dispose();
        _player = null;

        // Giải phóng buffer trong bộ nhớ.
        _buffer?.Dispose();
        _buffer = null;

        // Xóa URL hiện tại để phản ánh trạng thái đã dừng.
        CurrentUrl = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tải stream từ URL mạng.
    /// </summary>
    /// <param name="url">Địa chỉ audio từ internet.</param>
    /// <returns>Stream nếu tải thành công; ngược lại <c>null</c>.</returns>
    private static async Task<Stream?> GetStreamFromUrlAsync(string url)
    {
        try
        {
            // Tạo HttpClient tạm để lấy stream audio từ mạng.
            using var client = new HttpClient();
            return await client.GetStreamAsync(url);
        }
        catch { return null; }
    }
}
