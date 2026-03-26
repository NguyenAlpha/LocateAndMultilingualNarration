namespace Mobile.Models;

public class Stall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
}
