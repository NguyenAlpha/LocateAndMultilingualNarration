namespace Mobile.Models;

public class StallItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public double Distance { get; set; }
    public double Rating { get; set; }
    public string DistanceText => $"{Distance:F1} km";
    public string RatingText => Rating.ToString("F1");
}
