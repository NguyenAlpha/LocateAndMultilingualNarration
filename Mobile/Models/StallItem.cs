namespace Mobile.Models;

//public class StallItem
//{
//    public Guid Id { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public string Description { get; set; } = string.Empty;
//    public string ImageUrl { get; set; } = string.Empty;
//    public double Distance { get; set; }
//    public double Rating { get; set; }
//    public string DistanceText => $"{Distance:F1} km";
//    public string RatingText => Rating.ToString("F1");
//}


public class StallItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = "https://via.placeholder.com/300x200?text=No+Image"; // fallback
    public string BusinessName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // OLD CODE (kept for reference): public double Distance { get; set; }
    // OLD CODE (kept for reference): public double Rating { get; set; }

    // Khoảng cách từ người dùng (sẽ tính sau)
    public double DistanceInKm { get; set; } = 0;

    // Giữ lại để tương thích với code cũ dùng Distance/Rating.
    public double Distance
    {
        get => DistanceInKm;
        set => DistanceInKm = value;
    }

    // Giá trị đánh giá dùng cho UI.
    public double Rating { get; set; } = 4.5;

    // Computed properties cho UI
    public string DistanceText => DistanceInKm > 0
        ? $"{DistanceInKm:F1} km"
        : "Gần đây";

    public string RatingText => Rating.ToString("F1");

    public string StatusText => IsActive ? "Hoạt động" : "Tạm ngưng";
}