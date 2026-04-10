using Shared.DTOs.DevicePreferences;

namespace Mobile.Services;

/// <summary>
/// Lưu và đọc preference của thiết bị (ngôn ngữ, giọng đọc, tốc độ...)
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
    private const string KeySpeechRate          = "pref_speech_rate";
    private const string KeyAutoPlay            = "pref_auto_play";

    public void Save(DevicePreferenceDetailDto dto)
    {
        Preferences.Set(KeyLanguageId,          dto.LanguageId.ToString());
        Preferences.Set(KeyLanguageCode,        dto.LanguageCode);
        Preferences.Set(KeyLanguageName,        dto.LanguageName);
        Preferences.Set(KeyLanguageDisplayName, dto.LanguageDisplayName ?? string.Empty);
        Preferences.Set(KeyLanguageFlagCode,    dto.LanguageFlagCode    ?? string.Empty);
        Preferences.Set(KeyVoiceId,             dto.VoiceId?.ToString() ?? string.Empty);
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
        Preferences.Remove(KeySpeechRate);
        Preferences.Remove(KeyAutoPlay);
    }
}
