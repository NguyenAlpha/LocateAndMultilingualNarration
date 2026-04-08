using Microsoft.Maui.Storage;

namespace Mobile.Services;

/// <summary>
/// Helper lưu/đọc dữ liệu local vào Preferences (key-value storage cục bộ).
/// Dùng để lưu trữ thông tin người dùng (stallId, preferences...) mà không cần đến internet.
/// </summary>
public static class LocalStorageService
{
    // OLD CODE (kept for reference): private const string StallIdKey = "stallId";
    private const string StallKey = "StallId";

    // Key để lưu device preferences đã từng sync, tránh sync lại liên tục.
    private const string PreferencesSyncedKey = "preferences_synced";

    /// <summary>
    /// Lưu stallId vào Preferences khi user scan QR thành công.
    /// stallId sẽ được giữ lại cho đến khi user đăng xuất hoặc xoá dữ liệu app.
    /// </summary>
    // OLD CODE (kept for reference):
    // public static void SaveStallId(string stallId)
    // {
    //     if (string.IsNullOrWhiteSpace(stallId))
    //         return;
    //
    //     Preferences.Set(StallIdKey, stallId);
    // }
    public static Task SaveStallId(string stallId)
    {
        if (string.IsNullOrWhiteSpace(stallId))
        {
            Console.WriteLine("[DEBUG] Saved StallId: (empty)");
            return Task.CompletedTask;
        }

        Preferences.Set(StallKey, stallId);
        Console.WriteLine($"[DEBUG] Saved StallId: {stallId}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lấy stallId đã lưu từ lần scan QR trước.
    /// </summary>
    // OLD CODE (kept for reference):
    // public static string? GetStallId()
    // {
    //     return Preferences.Get(StallIdKey, null);
    // }
    public static Task<string> GetStallId()
    {
        var value = Preferences.Get(StallKey, string.Empty);
        Console.WriteLine($"[DEBUG] Loaded StallId: {value}");
        return Task.FromResult(value);
    }

    /// <summary>
    /// Kiểm tra xem user đã từng scan QR (đã lưu stallId) hay chưa.
    /// </summary>
    // OLD CODE (kept for reference):
    // public static bool HasStall()
    // {
    //     return !string.IsNullOrEmpty(GetStallId());
    // }
    public static Task<bool> HasStall()
    {
        var value = Preferences.Get(StallKey, string.Empty);
        var hasStall = !string.IsNullOrEmpty(value);
        Console.WriteLine($"[DEBUG] HasStall: {hasStall}");
        return Task.FromResult(hasStall);
    }

    /// <summary>
    /// Xoá stallId khi user logout hoặc chuyển sang thiết bị khác.
    /// </summary>
    // OLD CODE (kept for reference):
    // public static void ClearStallId()
    // {
    //     Preferences.Remove(StallIdKey);
    // }
    public static Task ClearStall()
    {
        Preferences.Remove(StallKey);
        Console.WriteLine("[DEBUG] Cleared StallId");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Đánh dấu đã sync device preferences thành công để tránh sync lại liên tục.
    /// </summary>
    public static void MarkPreferencesSynced()
    {
        Preferences.Set(PreferencesSyncedKey, true);
    }

    /// <summary>
    /// Kiểm tra xem device preferences đã được sync lên API hay chưa.
    /// </summary>
    public static bool IsPreferencesSynced()
    {
        return Preferences.Get(PreferencesSyncedKey, false);
    }

    /// <summary>
    /// Reset flag sync khi user chọn language/voice mới.
    /// </summary>
    public static void ClearPreferencesSyncFlag()
    {
        Preferences.Remove(PreferencesSyncedKey);
    }
}
