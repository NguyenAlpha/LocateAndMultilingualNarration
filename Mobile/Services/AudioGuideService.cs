using System.Net.Http;
using Plugin.Maui.Audio;

namespace Mobile.Services;

public interface IAudioGuideService
{
    Task PlayFromUrlAsync(string audioUrl, CancellationToken cancellationToken = default);
    void Pause();
    void Resume();
    void Stop();
    bool IsPlaying { get; }
}

public class AudioGuideService : IAudioGuideService
{
    private readonly IAudioManager _audioManager;
    private readonly IHttpClientFactory _httpClientFactory;

    private IAudioPlayer? _player;
    private MemoryStream? _buffer;

    public bool IsPlaying => _player?.IsPlaying ?? false;

    public AudioGuideService(IAudioManager audioManager, IHttpClientFactory httpClientFactory)
    {
        _audioManager = audioManager;
        _httpClientFactory = httpClientFactory;
    }

    public async Task PlayFromUrlAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            throw new InvalidOperationException("Audio URL không hợp lệ.");
        }

        Stop();

        var client = _httpClientFactory.CreateClient();
        await using var stream = await client.GetStreamAsync(audioUrl, cancellationToken);

        _buffer = new MemoryStream();
        await stream.CopyToAsync(_buffer, cancellationToken);
        _buffer.Position = 0;

        _player = _audioManager.CreatePlayer(_buffer);
        _player.Play();
    }

    public void Pause()
    {
        _player?.Pause();
    }

    public void Resume()
    {
        if (_player is { IsPlaying: false })
        {
            _player.Play();
        }
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _buffer?.Dispose();
        _buffer = null;
    }
}
