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
    /// Phát sinh khi audio kết thúc tự nhiên (không phát sinh khi dừng thủ công bằng StopAsync).
    /// </summary>
    event Action? PlaybackCompleted;

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
    private readonly SemaphoreSlim _playerLock = new(1, 1);

    /// <summary>
    /// Cho biết player hiện tại có đang phát hay không.
    /// </summary>
    public bool IsPlaying => _player?.IsPlaying ?? false;

    /// <summary>
    /// URL hoặc đường dẫn của audio đang được phát.
    /// </summary>
    public string? CurrentUrl { get; private set; }

    /// <summary>
    /// Phát sinh khi audio kết thúc tự nhiên.
    /// Không phát sinh khi gọi StopAsync() thủ công.
    /// </summary>
    public event Action? PlaybackCompleted;

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

        await _playerLock.WaitAsync();

        try
        {
            // Dừng audio cũ trước khi phát nội dung mới (không tái sử dụng lock).
            StopInternal();
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
            _player.PlaybackEnded += OnPlayerPlaybackEnded;
            _player.Play();
        }
        finally
        {
            _playerLock.Release();
        }
    }

    /// <summary>
    /// Tạm dừng audio hiện tại.
    /// Lấy <see cref="_playerLock"/> để tránh race với <see cref="PlayAsync"/> đang dispose player.
    /// </summary>
    public async Task PauseAsync()
    {
        await _playerLock.WaitAsync();
        try
        {
            _player?.Pause();
        }
        finally
        {
            _playerLock.Release();
        }
    }

    /// <summary>
    /// Tiếp tục phát audio đã tạm dừng.
    /// Lấy <see cref="_playerLock"/> để tránh race với <see cref="PlayAsync"/> đang dispose player.
    /// </summary>
    public async Task ResumeAsync()
    {
        await _playerLock.WaitAsync();
        try
        {
            if (_player is { IsPlaying: false })
                _player.Play();
        }
        finally
        {
            _playerLock.Release();
        }
    }

    /// <summary>
    /// Xử lý sự kiện kết thúc audio tự nhiên từ plugin.
    /// Chạy trên background thread — dispatch về main thread trước khi fire event ra ngoài.
    /// </summary>
    private void OnPlayerPlaybackEnded(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(() => PlaybackCompleted?.Invoke());

    /// <summary>
    /// Dừng audio hiện tại và giải phóng bộ nhớ. Lấy <see cref="_playerLock"/> để an toàn với Pause/Resume.
    /// </summary>
    public async Task StopAsync()
    {
        await _playerLock.WaitAsync();
        try
        {
            StopInternal();
        }
        finally
        {
            _playerLock.Release();
        }
    }

    /// <summary>
    /// Phiên bản không lock — chỉ gọi khi caller đã giữ <see cref="_playerLock"/>.
    /// </summary>
    private void StopInternal()
    {
        // Unsubscribe trước khi Stop để tránh fire PlaybackCompleted khi dừng thủ công.
        if (_player != null)
            _player.PlaybackEnded -= OnPlayerPlaybackEnded;

        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _buffer?.Dispose();
        _buffer = null;

        CurrentUrl = null;
    }

    /// <summary>
    /// Tải audio từ URL mạng về bộ nhớ. Trả về <c>null</c> nếu không có mạng hoặc lỗi.
    /// </summary>
    /// <param name="url">Địa chỉ audio từ internet.</param>
    /// <returns>MemoryStream chứa toàn bộ audio nếu thành công; ngược lại <c>null</c>.</returns>
    private static async Task<Stream?> GetStreamFromUrlAsync(string url)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // GetByteArrayAsync tải toàn bộ rồi trả MemoryStream — tránh CopyToAsync treo UI.
            var bytes = await client.GetByteArrayAsync(url, cts.Token);
            return new MemoryStream(bytes);
        }
        catch { return null; }
    }
}
