namespace Mobile;

/// <summary>
/// Cấu hình URL API cho Mobile App
/// Vì bạn và đồng nghiệp không chung mạng LAN, nên ưu tiên dùng Azure Production
/// </summary>
public static class DevConfig
{
    /// <summary>
    /// URL API sẽ được sử dụng
    /// - DEBUG: Mặc định dùng Azure Production để test trên điện thoại thật
    /// - Bạn có thể tạm đổi sang IP LAN nếu cần test nhanh khi cùng mạng
    /// </summary>
    public static string ApiBaseUrl
    {
        get
        {
#if DEBUG
            // ==================== DEBUG MODE ====================
            // Hiện tại ưu tiên dùng Azure vì hai máy khác mạng
            return ProductionApiBaseUrl;

            // Nếu sau này cùng mạng với máy chạy API, uncomment dòng dưới và comment dòng trên:
            // return LocalApiBaseUrl;
#else
            // Release mode luôn dùng Production
            return ProductionApiBaseUrl;
#endif
        }
    }

    /// <summary>
    /// URL Production trên Azure (đây là máy chủ chung)
    /// </summary>
    public const string ProductionApiBaseUrl =
        "https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net";

    /// <summary>
    /// URL Local (dùng khi bạn và máy chạy API cùng một mạng WiFi)
    /// </summary>
    public const string LocalApiBaseUrl = "http://192.168.1.10:5299";   // ← Thay IP này khi cần

    /// <summary>
    /// Thông tin debug để biết đang dùng URL nào
    /// </summary>
    public static string GetCurrentInfo()
    {
#if DEBUG
        return $"[DEBUG] Using Production Azure: {ApiBaseUrl}";
#else
        return $"[RELEASE] Using Production Azure: {ApiBaseUrl}";
#endif
    }
}