namespace Mobile;

/// <summary>
/// Cấu hình môi trường dev — thay đổi tại đây khi chuyển giữa emulator và máy thật.
/// </summary>
public static class DevConfig
{
    /// <summary>
    /// URL gốc của API backend.
    /// - Emulator Android  : "http://10.0.2.2:5299"
    /// - Máy thật (USB/WiFi): "http://&lt;IP_LAPTOP&gt;:5299"
    ///   Tìm IP laptop: chạy `ipconfig` → IPv4 Address của adapter WiFi/Ethernet
    ///   Ví dụ: "http://192.168.1.5:5299"
    /// </summary>
    public const string ApiBaseUrl = "https://locateandmultilingualnarration-amgrfua6fbd7gnce.eastasia-01.azurewebsites.net";
}
