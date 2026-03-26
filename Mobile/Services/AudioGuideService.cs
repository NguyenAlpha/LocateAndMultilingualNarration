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

    public bool IsPlaying => _player?.IsPlaying ?? false;
    public string? CurrentUrl { get; private set; }

    public AudioGuideService(IAudioManager audioManager)
        => _audioManager = audioManager;

    public async Task PlayAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        await StopAsync(); // dừng audio cũ nếu có
        CurrentUrl = url;

        var stream = await GetStreamFromUrlAsync(url);
        if (stream is null)
        {
            CurrentUrl = null;
            return;
        }

        _buffer = new MemoryStream();
        await stream.CopyToAsync(_buffer);
        _buffer.Position = 0;

        _player = _audioManager.CreatePlayer(_buffer);
        _player.Play();
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
            return await client.GetStreamAsync(url);
        }
        catch { return null; }
    }
}
