using Plugin.Maui.Audio;

namespace Mobile.Services;

public interface IAudioGuideService
{
    bool IsPlaying { get; }
    string? CurrentUrl { get; }
    Task PlayAsync(string url);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
}

public class AudioGuideService : IAudioGuideService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    private MemoryStream? _buffer;
    private readonly SemaphoreSlim _playerLock = new(1, 1);

    // Check if player is actively playing
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public string? CurrentUrl { get; private set; }

    public AudioGuideService(IAudioManager audioManager)
        => _audioManager = audioManager;

    public async Task PlayAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        await _playerLock.WaitAsync();

        try
        {
            await StopAsync(); // dừng audio cũ nếu có
            CurrentUrl = url;

            Stream? stream;

            // Local file path → đọc từ disk, không cần mạng
            // Remote URL → stream từ network
            if (File.Exists(url))
                stream = File.OpenRead(url);
            else
                stream = await GetStreamFromUrlAsync(url);

            if (stream is null)
            {
                CurrentUrl = null;
                return;
            }

            _buffer = new MemoryStream();
            await stream.CopyToAsync(_buffer);
            await stream.DisposeAsync();
            _buffer.Position = 0;

            _player = _audioManager.CreatePlayer(_buffer);
            _player.Play();
        }
        finally
        {
            _playerLock.Release();
        }
    }

    public Task PauseAsync()
    {
        _player?.Pause();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_player is { IsPlaying: false })
            _player.Play();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _buffer?.Dispose();
        _buffer = null;

        CurrentUrl = null;
        return Task.CompletedTask;
    }

    private static async Task<Stream?> GetStreamFromUrlAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            return await client.GetStreamAsync(url);
        }
        catch { return null; }
    }
}
