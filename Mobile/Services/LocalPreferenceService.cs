using System.Text.Json;
using Shared.DTOs.DevicePreferences;

namespace Mobile.Services;

/// <summary>
/// Lưu và đọc preference của thiết bị (ngôn ngữ, giọng đọc, tốc độ, tour đang chạy...)
/// từ Preferences cục bộ — không cần mạng.
/// </summary>
public interface ILocalPreferenceService
{
    /// <summary>Lưu preference sau khi đồng bộ thành công với API.</summary>
    void Save(DevicePreferenceDetailDto dto);

    /// <summary>Đọc preference đã lưu. Trả về null nếu chưa có.</summary>
    DevicePreferenceDetailDto? Load();

    /// <summary>Xóa toàn bộ preference đã lưu (vd: khi reset thiết bị).</summary>
    void Clear();

    // ====== Tour progress ======

    /// <summary>Lấy tour đang chạy (null nếu không có).</summary>
    Guid? GetActiveTourId();

    /// <summary>Đặt tour đang chạy; truyền null để clear.</summary>
    void SetActiveTourId(Guid? tourId);

    /// <summary>Lấy danh sách stall đã hoàn tất của tour đang chạy.</summary>
    HashSet<Guid> GetCompletedStops();

    /// <summary>Đánh dấu 1 stall đã hoàn tất.</summary>
    void AddCompletedStop(Guid stallId);

    /// <summary>Xóa toàn bộ tiến độ tour (active tour + completed stops).</summary>
    void ClearTourProgress();
}

/// <summary>
/// Triển khai <see cref="ILocalPreferenceService"/> dùng MAUI Preferences.
/// Dữ liệu tồn tại xuyên suốt các lần mở app, mất khi gỡ cài đặt.
/// </summary>
public class LocalPreferenceService : ILocalPreferenceService
{
    private const string KeyLanguageId          = "pref_language_id";
    private const string KeyLanguageCode        = "pref_language_code";
    private const string KeyLanguageName        = "pref_language_name";
    private const string KeyLanguageDisplayName = "pref_language_display_name";
    private const string KeyLanguageFlagCode    = "pref_language_flag_code";
    private const string KeyVoiceId             = "pref_voice_id";
    private const string KeyVoiceDisplayName    = "pref_voice_display_name";
    private const string KeySpeechRate          = "pref_speech_rate";
    private const string KeyAutoPlay            = "pref_auto_play";

    private const string KeyActiveTourId        = "pref_active_tour_id";
    private const string KeyCompletedStops      = "pref_tour_completed_stops";

    public void Save(DevicePreferenceDetailDto dto)
    {
        Preferences.Set(KeyLanguageId,          dto.LanguageId.ToString());
        Preferences.Set(KeyLanguageCode,        dto.LanguageCode);
        Preferences.Set(KeyLanguageName,        dto.LanguageName);
        Preferences.Set(KeyLanguageDisplayName, dto.LanguageDisplayName ?? string.Empty);
        Preferences.Set(KeyLanguageFlagCode,    dto.LanguageFlagCode    ?? string.Empty);
        Preferences.Set(KeyVoiceId,             dto.VoiceId?.ToString() ?? string.Empty);
        Preferences.Set(KeyVoiceDisplayName,    dto.VoiceDisplayName    ?? string.Empty);
        Preferences.Set(KeySpeechRate,          dto.SpeechRate.ToString());
        Preferences.Set(KeyAutoPlay,            dto.AutoPlay);
    }

    public DevicePreferenceDetailDto? Load()
    {
        var languageIdStr  = Preferences.Get(KeyLanguageId,   null);
        var languageCode   = Preferences.Get(KeyLanguageCode, null);

        // Chưa lưu lần nào → trả về null
        if (string.IsNullOrEmpty(languageIdStr) || string.IsNullOrEmpty(languageCode))
            return null;

        if (!Guid.TryParse(languageIdStr, out var languageId))
            return null;

        var voiceIdStr = Preferences.Get(KeyVoiceId, null);
        Guid? voiceId  = Guid.TryParse(voiceIdStr, out var v) ? v : null;

        _ = decimal.TryParse(Preferences.Get(KeySpeechRate, "1.0"), out var speechRate);

        return new DevicePreferenceDetailDto
        {
            LanguageId          = languageId,
            LanguageCode        = languageCode,
            LanguageName        = Preferences.Get(KeyLanguageName,        string.Empty)!,
            LanguageDisplayName = Preferences.Get(KeyLanguageDisplayName, null),
            LanguageFlagCode    = Preferences.Get(KeyLanguageFlagCode,    null),
            VoiceId             = voiceId,
            VoiceDisplayName    = Preferences.Get(KeyVoiceDisplayName, null),
            SpeechRate          = speechRate <= 0 ? 1.0m : speechRate,
            AutoPlay            = Preferences.Get(KeyAutoPlay, true)
        };
    }

    public void Clear()
    {
        Preferences.Remove(KeyLanguageId);
        Preferences.Remove(KeyLanguageCode);
        Preferences.Remove(KeyLanguageName);
        Preferences.Remove(KeyLanguageDisplayName);
        Preferences.Remove(KeyLanguageFlagCode);
        Preferences.Remove(KeyVoiceId);
        Preferences.Remove(KeyVoiceDisplayName);
        Preferences.Remove(KeySpeechRate);
        Preferences.Remove(KeyAutoPlay);
        ClearTourProgress();
    }

    // ====== Tour progress ======

    public Guid? GetActiveTourId()
    {
        var raw = Preferences.Get(KeyActiveTourId, null);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public void SetActiveTourId(Guid? tourId)
    {
        if (tourId is null)
            Preferences.Remove(KeyActiveTourId);
        else
            Preferences.Set(KeyActiveTourId, tourId.Value.ToString());
    }

    public HashSet<Guid> GetCompletedStops()
    {
        var raw = Preferences.Get(KeyCompletedStops, null);
        if (string.IsNullOrEmpty(raw)) return new HashSet<Guid>();
        try
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(raw);
            return ids is null ? new HashSet<Guid>() : new HashSet<Guid>(ids);
        }
        catch
        {
            return new HashSet<Guid>();
        }
    }

    public void AddCompletedStop(Guid stallId)
    {
        var set = GetCompletedStops();
        if (set.Add(stallId))
        {
            Preferences.Set(KeyCompletedStops, JsonSerializer.Serialize(set.ToList()));
        }
    }

    public void ClearTourProgress()
    {
        Preferences.Remove(KeyActiveTourId);
        Preferences.Remove(KeyCompletedStops);
    }
}
