namespace Mobile.Models;

/// <summary>
/// DTO ngôn ngữ dùng cho UI ProfilePage (độc lập với Shared DTO để tránh phụ thuộc tầng API).
/// </summary>
public class LanguageDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Flag { get; set; } = "🌐";
    public bool IsActive { get; set; }
}
